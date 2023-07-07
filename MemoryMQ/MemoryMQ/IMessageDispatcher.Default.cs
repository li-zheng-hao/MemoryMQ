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
    private readonly IPersistStorage _persistStorage;
    private readonly IServiceProvider _serviceProvider;

    private ConcurrentDictionary<string, Channel<IMessage>> _channels =
        new ConcurrentDictionary<string, Channel<IMessage>>();

    private Dictionary<string, IMessageConsumer> _consumers = new Dictionary<string, IMessageConsumer>();

    public DefaultMessageDispatcher(ILogger<DefaultMessageDispatcher> logger, IOptions<MemoryMQOptions> options,
        IPersistStorage persistStorage,
        IServiceProvider serviceProvider)
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

        RunDispatch(cancellationToken);
    }

    private void RunDispatch(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
        {
            for (int i = 0; i < consumer.Value.ParallelNum; i++)
            {
                Task.Factory.StartNew(async () => await PollingMessage(consumer, cancellationToken), cancellationToken);
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
                    await consumer.Value.ReceivedAsync(message);
                    if(_options.Value.EnablePersistent)
                        await _persistStorage.RemoveAsync(message);
                    
                }

                await Task.Delay(_options.Value.PollingInterval, cancellationToken);
            }
            catch (Exception e)
            {
               _logger.LogError(e,"polling message error");
            }
           
        }
    }

    private async Task RestoreMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _persistStorage.RestoreAsync();
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
            if (_channels.ContainsKey(consumer.Topic))
                continue;

            var channel = Channel.CreateBounded<IMessage>(new BoundedChannelOptions(_options.Value.GlobalMaxChannelSize)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = _options.Value.GlobalBoundedChannelFullMode
            });

            _consumers.Add(consumer.Topic, consumer);

            _logger.LogDebug($"Add channel for topic {consumer.Topic}");

            _channels.TryAdd(consumer.Topic, channel);
        }
    }

    public Task StopDispatchAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Enqueue(IMessage message)
    {
        if (string.IsNullOrEmpty(message.GetTopic()))
            throw new ArgumentException("message topic should not be empty!");

        if (string.IsNullOrEmpty(message.GetMessageId()))
            throw new ArgumentException("message id should not be empty!");

        _channels.TryGetValue(message.GetTopic()!, out var channel);

        channel!.Writer.TryWrite(message);
    }
}