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
    }


    public Task StopDispatchAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async ValueTask<bool> EnqueueAsync(IMessage message)
    {
        if (string.IsNullOrEmpty(message.GetTopic()))
            throw new ArgumentException("message topic should not be empty!");

        if (string.IsNullOrEmpty(message.GetMessageId()))
            throw new ArgumentException("message id should not be empty!");

        if (string.IsNullOrEmpty(message.GetCreateTime()))
            throw new ArgumentException("message create time should not be empty!");


        if (_options.Value.EnablePersistent && message.GetRetryCount() is null or 0)
            await _persistStorage!.AddAsync(message);

        _channels.TryGetValue(message.GetTopic()!, out var channel);

        if (channel is null)
        {
            _logger.LogWarning("message topic {Topic} not found matched channel, please check your consumer registration!", message.GetTopic());

            return false;
        }

        return channel.Writer.TryWrite(message);
    }

    public async Task StartDispatchAsync(CancellationToken cancellationToken)
    {
        InitConsumers();

        if (_options.Value.EnablePersistent)
            await RestoreMessagesAsync(cancellationToken);

        RunDispatch(cancellationToken);
    }

    private void RunDispatch(CancellationToken cancellationToken)
    {
        foreach (var consumerOptions in _consumerFactory.ConsumerOptions)
        {
            for (var i = 0; i < consumerOptions.Value.ParallelNum; i++)
            {
                Task.Factory.StartNew(async () => await PollingMessage(consumerOptions.Value, cancellationToken), cancellationToken,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }

    private async Task PollingMessage(MessageOptions messageOptions,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _channels.TryGetValue(messageOptions.Topic, out var channel);

                while (channel!.Reader.CanPeek && channel.Reader.TryRead(out var message))
                {
                    using var scope = _serviceProvider.CreateScope();

                    var consumer = _consumerFactory.CreateConsumer(scope.ServiceProvider, messageOptions.Topic);

                    if (consumer is null)
                    {
                        _logger.LogWarning("{MessageOptionsTopic} consumer is null", messageOptions.Topic);

                        continue;
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
                            await _retryStrategy.ScheduleRetry(message, consumer, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "retry message error {MessageOptionsTopic}", messageOptions.Topic);
                    }
                }

                await Task.Delay(_options.Value.PollingInterval, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "poll and consume message error");
            }
        }
    }

    private async Task RestoreMessagesAsync(CancellationToken cancellationToken)
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

        foreach (var consumerOption in consumerOptions)
        {
            if (_channels.ContainsKey(consumerOption.Key))
                continue;

            var channel = Channel.CreateBounded<IMessage>(new BoundedChannelOptions(_options.Value.GlobalMaxChannelSize)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = _options.Value.GlobalBoundedChannelFullMode
            });

            _logger.LogDebug("Add channel for topic {ConfigTopic}", consumerOption.Key);

            _channels.TryAdd(consumerOption.Key, channel);
        }
    }

    private async Task RetryMessageAsync(IMessage msg)
    {
        var retry = await EnqueueAsync(msg);

        if (!retry)
        {
            _logger.LogError("message {MessageId} enqueue failed", msg.GetMessageId());
        }
    }
}