using System.Collections.Concurrent;
using System.Threading.Channels;
using EasyCompressor;
using MemoryMQ.Compress;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Internal;
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

    private readonly ICompressor _compressor;

    private readonly ConcurrentDictionary<string, Channel<IMessage>> _channels = new();

    private CancellationToken _cancelToken;

    public DefaultMessageDispatcher(ILogger<DefaultMessageDispatcher> logger, IOptions<MemoryMQOptions> options,
        IServiceProvider serviceProvider,
        IConsumerFactory consumerFactory,
        IRetryStrategy retryStrategy,
        ICompressor compressor=null,
        IPersistStorage persistStorage = null)
    {
        _logger = logger;
        _options = options;
        _persistStorage = persistStorage;
        _serviceProvider = serviceProvider;
        _consumerFactory = consumerFactory;
        _retryStrategy = retryStrategy;
        _compressor = compressor;

        retryStrategy.MessageRetryEvent = RetryMessageAsync;
        retryStrategy.MessageRetryFailureEvent = RetryMessageFailureAsync;
    }

    public Task StopDispatchAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async ValueTask<bool> EnqueueAsync(IMessage message,bool isNewMessage = true)
    {
        _channels.TryGetValue(message.GetTopic()!, out var channel);

        if (channel is null)
        {
            _logger.LogWarning("message topic {Topic} not found matched channel, please check your consumer registration!", message.GetTopic());

            return false;
        }

        if (isNewMessage&&_consumerFactory.ConsumerOptions[message.GetTopic()].GetEnableCompression(_options.Value))
        {
            message.Body = _compressor.Compress(message.Body);
        }
        
        if (_consumerFactory.ConsumerOptions[message.GetTopic()].GetEnablePersistence(_options.Value) && message.GetRetryCount() is null or 0)
        {
            var opResult = await _persistStorage!.AddAsync(message);

            if (!opResult)
            {
                _logger.LogWarning("message {MessageId} enqueue to persistent storage failed!", message.GetMessageId());

                return false;
            }
        }
        
        // use WaitToWriteAsync internally to guarantee message will be write to channel
        await channel.Writer.WriteAsync(message,_cancelToken);

        return true;
    }

    public async ValueTask<bool> EnqueueAsync(ICollection<IMessage> messages,bool isNewMessage = true)
    {

        foreach (var message in messages)
        {
            if (!_channels.ContainsKey(message.GetTopic()))
            {
                _logger.LogWarning("message topic {Topic} not found matched channel, please check your consumer registration!", message.GetTopic());

                return false;
            }

            if (isNewMessage&&_consumerFactory.ConsumerOptions[message.GetTopic()].GetEnableCompression(_options.Value))
            {
                message.Body = _compressor.Compress(message.Body);
            }
        }

        var persistMessages = messages.Where(it => it.GetRetryCount() is null or 0).ToList();

        if (_options.Value.EnablePersistence && persistMessages.Any())
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
            await _channels[message.GetTopic()].Writer.WriteAsync(message,_cancelToken);
        }

        return true;
    }

    public async Task StartDispatchAsync(CancellationToken cancellationToken)
    {
        _cancelToken = cancellationToken;

        if (_options.Value.EnablePersistence && _persistStorage is not null)
            await _persistStorage.CreateTableAsync();

        InitConsumers();

        if (_options.Value.EnablePersistence)
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
                if (_options.Value.EnablePersistence) await _persistStorage!.RemoveAsync(message);
            }
            else
            {
                message.IncreaseRetryCount();

                await _retryStrategy.ScheduleRetryAsync(message, consumer.GetMessageConfig(), cancellationToken);
            }

        }
        catch (Exception e)
        {
            _logger.LogError(e, "retry message error {MessageOptionsTopic}", messageOptions.Topic);
        }
    }

    async internal Task RestoreMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var messages = await _persistStorage!.RestoreAsync();

            foreach (var message in messages)
            {
                if (!_channels.ContainsKey(message.GetTopic()!))
                {
                    _logger.LogWarning("message {MessageId} topic {Topic} not found matched channel", message.GetMessageId(), message.GetTopic());

                    continue;
                }
            
                if (_options.Value.EnableCompression)
                    message.Body = _compressor.Decompress(message.Body);
            
                var channel = _channels[message.GetTopic()!];

                await channel.Writer.WriteAsync(message, cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,"restore messages error");
        }
        
    }

    private void InitConsumers()
    {
        var consumerOptions = _consumerFactory.ConsumerOptions;

        foreach (var messageOption in consumerOptions.Select(it => it.Value))
        {
            if (_channels.ContainsKey(messageOption.Topic))
                continue;

            var channel = Channel.CreateBounded<IMessage>(new BoundedChannelOptions(messageOption.GetMaxChannelSize(_options.Value))
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = messageOption.GetBoundedChannelFullMode(_options.Value)
            });

            _logger.LogDebug("Add channel for topic {ConfigTopic}", messageOption.Topic);

            _channels.TryAdd(messageOption.Topic, channel);
        }
    }

    async internal Task RetryMessageAsync(IMessage msg)
    {
        var retry = await EnqueueAsync(msg,false);

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