using System.Collections.Concurrent;
using System.Threading.Channels;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;
using MemoryMQ.RetryStrategy;
using MemoryMQ.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Dispatcher;

public class DefaultMessageDispatcher : IMessageDispatcher
{
    private readonly ILogger<DefaultMessageDispatcher> _logger;

    private readonly IOptions<MemoryMQOptions> _options;

    private readonly IPersistStorage? _persistStorage;

    private readonly IServiceProvider _serviceProvider;

    private readonly IConsumerFactory _consumerFactory;

    private readonly IRetryStrategy _retryStrategy;

    private readonly ConcurrentDictionary<string, Channel<IMessage>> _channels = new();

    private CancellationToken _cancelToken;

    public DefaultMessageDispatcher(ILogger<DefaultMessageDispatcher> logger, IOptions<MemoryMQOptions> options,
        IServiceProvider serviceProvider,
        IConsumerFactory consumerFactory,
        IRetryStrategy retryStrategy,
        IPersistStorage persistStorage = null)
    {
        _logger = logger;
        _options = options;
        _persistStorage = persistStorage;
        _serviceProvider = serviceProvider;
        _consumerFactory = consumerFactory;
        _retryStrategy = retryStrategy;

        retryStrategy.MessageRetryEvent = RetryMessageAsync;
        retryStrategy.MessageRetryFailureEvent = RetryMessageFailureAsync;
    }

    public Task StopDispatchAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async ValueTask<bool> EnqueueAsync(IMessage message)
    {
        _channels.TryGetValue(message.GetTopic()!, out var channel);

        if (channel is null)
        {
            _logger.LogWarning("message topic {Topic} not found matched channel, please check your consumer registration!", message.GetTopic());

            return false;
        }

        if (_options.Value.EnablePersistent && message.GetRetryCount() is null or 0)
        {
            var opResult = await _persistStorage!.AddAsync(message);

            if (!opResult)
            {
                _logger.LogWarning("message {MessageId} enqueue to persistent storage failed!", message.GetMessageId());

                return false;
            }
        }

        return channel.Writer.TryWrite(message);
    }

    public async ValueTask<bool> EnqueueAsync(IEnumerable<IMessage> messages)
    {
        string topic = messages.First().GetTopic();
        
        _channels.TryGetValue(topic, out var channel);

        if (channel is null)
        {
            _logger.LogWarning("message topic {Topic} not found matched channel, please check your consumer registration!", topic);

            return false;
        }

        var persistMessages = messages.Where(it => it.GetRetryCount() is null or 0).ToList();

        if (_options.Value.EnablePersistent && persistMessages.Any())
        {
            var opResult = await _persistStorage!.AddAsync(persistMessages);

            if (!opResult)
            {
                _logger.LogWarning("messages enqueue to persistent storage failed!");

                return false;
            }
        }

        foreach (var message in messages)
        {
            bool opResult;
            int retryCount = 0;
            do
            {
                // retry 3 times
                opResult = channel.Writer.TryWrite(message);
            } while (!opResult && retryCount++ <= 3);
        }

        return true;
    }

    public async Task StartDispatchAsync(CancellationToken cancellationToken)
    {
        _cancelToken = cancellationToken;

        if (_options.Value.EnablePersistent && _persistStorage is not null)
            await _persistStorage.CreateTableAsync();

        InitConsumers();

        if (_options.Value.EnablePersistent)
            await RestoreMessagesAsync(cancellationToken);

        RunDispatch(cancellationToken);
    }

    private void RunDispatch(CancellationToken cancellationToken)
    {
        foreach (var consumerOption in _consumerFactory.ConsumerOptions.Select(it => it.Value))
        {
            for (var i = 0; i < consumerOption.ParallelNum; i++)
            {
                Task.Factory.StartNew(async () => await PollingMessage(consumerOption, cancellationToken), cancellationToken,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }

    async internal Task PollingMessage(MessageOptions messageOptions,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _channels.TryGetValue(messageOptions.Topic, out var channel);

                while (channel!.Reader.CanPeek && channel.Reader.TryRead(out var message))
                {
                    await ConsumeMessage(message, messageOptions, cancellationToken);
                }

                await Task.Delay(_options.Value.PollingInterval, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "poll and consume message error");
            }
        }
    }

    async internal Task ConsumeMessage(IMessage message, MessageOptions messageOptions, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var consumer = _consumerFactory.CreateConsumer(scope.ServiceProvider, messageOptions.Topic);

        if (consumer is null)
        {
            _logger.LogWarning("{MessageOptionsTopic} consumer is null", messageOptions.Topic);

            return;
        }

        bool isConsumeSuccess = true;

        try
        {
            await consumer.ReceivedAsync(message, cancellationToken);
        }
        catch (Exception e)
        {
            isConsumeSuccess = false;

            _logger.LogError(e, "consumer received message error");
        }

        try
        {
            if (isConsumeSuccess)
            {
                if (_options.Value.EnablePersistent) await _persistStorage!.RemoveAsync(message);
            }
            else
                await _retryStrategy.ScheduleRetryAsync(message, consumer.GetMessageConfig(), cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "retry message error {MessageOptionsTopic}", messageOptions.Topic);
        }
    }

    async internal Task RestoreMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _persistStorage!.RestoreAsync();

        foreach (var message in messages)
        {
            if (!_channels.ContainsKey(message.GetTopic()!))
            {
                _logger.LogWarning("message {MessageId} topic {Topic} not found matched channel", message.GetMessageId(), message.GetTopic());

                continue;
            }

            var channel = _channels[message.GetTopic()!];

            await channel.Writer.WriteAsync(message, cancellationToken);
        }
    }

    private void InitConsumers()
    {
        var consumerOptions = _consumerFactory.ConsumerOptions;

        foreach (var topic in consumerOptions.Select(it => it.Key))
        {
            if (_channels.ContainsKey(topic))
                continue;

            var channel = Channel.CreateBounded<IMessage>(new BoundedChannelOptions(_options.Value.GlobalMaxChannelSize)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = _options.Value.GlobalBoundedChannelFullMode
            });

            _logger.LogDebug("Add channel for topic {ConfigTopic}", topic);

            _channels.TryAdd(topic, channel);
        }
    }

    async internal Task RetryMessageAsync(IMessage msg)
    {
        var retry = await EnqueueAsync(msg);

        if (!retry)
        {
            _logger.LogError("message {MessageId} enqueue failed", msg.GetMessageId());
        }
    }

    async internal Task RetryMessageFailureAsync(IMessage message)
    {
        using var scope = _serviceProvider.CreateScope();

        var consumer = _consumerFactory.CreateConsumer(scope.ServiceProvider, message.GetTopic());

        if (consumer is null)
        {
            _logger.LogWarning("{MessageOptionsTopic} consumer is null", message.GetTopic());

            return;
        }

        try
        {
            await consumer.FailureRetryAsync(message, _cancelToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "consumer received message error");
        }
    }
}