using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryMQ;

public class DefaultMessageDispatcher : IMessageDispatcher
{
    private readonly ILogger<DefaultMessageDispatcher> _logger;
    private readonly IOptions<MemoryMQOptions> _options;
    private readonly IPersistStorage? _persistStorage;
    private readonly IServiceProvider _serviceProvider;

    private ConcurrentDictionary<string, Channel<IMessage>> _channels =
        new ConcurrentDictionary<string, Channel<IMessage>>();

    private Dictionary<string, IMessageConsumer> _consumers = new Dictionary<string, IMessageConsumer>();

    /// <summary>
    /// 重试队列 如果重启则该队列会清空 所有消息会重新开始消费
    /// </summary>
    private PriorityQueue<IMessage, long> _retryChannel = new PriorityQueue<IMessage, long>();

    public DefaultMessageDispatcher(ILogger<DefaultMessageDispatcher> logger, IOptions<MemoryMQOptions> options,
        IServiceProvider serviceProvider,
        IPersistStorage? persistStorage = null)
    {
        _logger = logger;
        _options = options;
        _persistStorage = persistStorage;
        _serviceProvider = serviceProvider;
    }

    public async Task StartDispatchAsync(CancellationToken cancellationToken)
    {
        InitConsumers();

        if (_options.Value.EnablePersistent)
            await RestoreMessagesAsync(cancellationToken);

        RunRetry(cancellationToken);

        RunDispatch(cancellationToken);
    }

    private void RunRetry(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (_retryChannel.TryPeek(out var message,out var ticks))
                {
                    // TODO get messages which retry time is less than now
                }
            }
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void RunDispatch(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
        {
            for (int i = 0; i < consumer.Value.Config.ParallelNum; i++)
            {
                Task.Factory.StartNew(async () => await PollingMessage(consumer, cancellationToken), cancellationToken,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }

    private async Task PollingMessage(KeyValuePair<string, IMessageConsumer> consumer,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _channels.TryGetValue(consumer.Key, out var channel);

                while (channel!.Reader.CanPeek && channel.Reader.TryRead(out var message))
                {
                    bool isConsumeSuccess = true;
                    try
                    {
                        await consumer.Value.ReceivedAsync(message, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        isConsumeSuccess = false;

                        _logger.LogError(e, "consumer received message error");
                    }

                    await Retry(isConsumeSuccess, message, consumer.Value, cancellationToken);
                }

                await Task.Delay(_options.Value.PollingInterval, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "poll and consume message error");
            }
        }
    }

    private async Task Retry(bool isConsumeSuccess, IMessage message, IMessageConsumer consumer,
        CancellationToken cancellationToken)
    {
        if (isConsumeSuccess)
        {
            if (_options.Value.EnablePersistent)
                await _persistStorage!.RemoveAsync(message);
        }
        else
        {
            message.IncreaseRetryCount();

            var retryCount = consumer.Config.RetryCount ?? _options.Value.GlobalRetryCount;

            // dont need retry
            if (!retryCount.HasValue || retryCount.Value == 0)
            {
                if (_options.Value.EnablePersistent)
                    await _persistStorage!.RemoveAsync(message);
                return;
            }

            // retry failure
            if (message.GetRetryCount() > retryCount)
            {
                try
                {
                    await consumer.FailureRetryAsync(message, cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "consumer process failure message error");
                }
                finally
                {
                    if (_options.Value.EnablePersistent)
                        await _persistStorage!.RemoveAsync(message);
                }
            }
            // retry again
            else
            {
                // todo : add delay
                EnqueueRetryQueue(message);

                if (_options.Value.EnablePersistent)
                {
                    await _persistStorage!.UpdateRetryAsync(message);
                }
            }
        }
    }

    private void EnqueueRetryQueue(IMessage message)
    {
        var retryCount = message.GetRetryCount()!;

        TimeSpan delay;

        switch (_options.Value.RetryMode)
        {
            case RetryMode.Fixed:
                delay = _options.Value.RetryInterval;
                break;
            case RetryMode.Exponential:
                delay = _options.Value.RetryInterval * Math.Pow(2, retryCount.Value);
                break;
            case RetryMode.Incremental:
                delay = _options.Value.RetryInterval * retryCount.Value;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _retryChannel.Enqueue(message, DateTime.UtcNow.Add(delay).Ticks);
    }

    private async Task RestoreMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _persistStorage!.RestoreAsync();

        foreach (var message in messages)
        {
            if (!_channels.ContainsKey(message.GetTopic()))
            {
                _logger.LogWarning(
                    $"message {message.GetMessageId()} topic {message.GetTopic()} not found matched channel");

                continue;
            }

            var channel = _channels[message.GetTopic()];

            await channel.Writer.WriteAsync(message, cancellationToken);
        }
    }

    private void InitConsumers()
    {
        var consumers = _serviceProvider.GetServices<IMessageConsumer>();

        foreach (var consumer in consumers)
        {
            if (_channels.ContainsKey(consumer.Config.Topic))
                continue;

            var channel = Channel.CreateBounded<IMessage>(new BoundedChannelOptions(_options.Value.GlobalMaxChannelSize)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = _options.Value.GlobalBoundedChannelFullMode
            });

            _consumers.Add(consumer.Config.Topic, consumer);

            _logger.LogDebug($"Add channel for topic {consumer.Config.Topic}");

            _channels.TryAdd(consumer.Config.Topic, channel);
        }
    }

    public Task StopDispatchAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async ValueTask EnqueueAsync(IMessage message)
    {
        if (string.IsNullOrEmpty(message.GetTopic()))
            throw new ArgumentException("message topic should not be empty!");

        if (string.IsNullOrEmpty(message.GetMessageId()))
            throw new ArgumentException("message id should not be empty!");

        if (string.IsNullOrEmpty(message.GetCreateTime()))
            throw new ArgumentException("message create time should not be empty!");


        if (_options.Value.EnablePersistent)
            await _persistStorage!.AddAsync(message);

        _channels.TryGetValue(message.GetTopic()!, out var channel);

        if (channel is null)
            throw new ArgumentException(
                $"message topic {message.GetTopic()} not found matched channel, please check your consumer registration!");

        channel!.Writer.TryWrite(message);
    }
}