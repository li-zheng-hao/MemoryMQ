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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Dispatcher;

public class DefaultMessageQueueManager : IMessageQueueManager
{
    private readonly ConcurrentDictionary<string, Channel<IMessage>> _channels;

    private readonly ILogger<DefaultMessageQueueManager> _logger;

    private readonly IConsumerFactory _consumerFactory;

    private readonly IOptions<MemoryMQOptions> _options;

    private readonly ICompressor? _compressor;

    private readonly IPersistStorage? _persistStorage;

    public Func<
        IMessage,
        MessageOptions,
        CancellationToken,
        Task
    >? MessageReceivedEvent { get; set; }

    public DefaultMessageQueueManager(
        ILogger<DefaultMessageQueueManager> logger,
        IConsumerFactory                    consumerFactory,
        IOptions<MemoryMQOptions>           options,
        ICompressor?                        compressor     = null,
        IPersistStorage?                    persistStorage = null
    )
    {
        _logger          = logger;
        _consumerFactory = consumerFactory;
        _options         = options;
        _compressor      = compressor;
        _persistStorage  = persistStorage;
        _channels        = new();
    }

    public List<string> InitConsumers()
    {
        var consumerOptions = _consumerFactory.ConsumerOptions;

        List<string> topics = new List<string>();

        foreach (var messageOption in consumerOptions.Select(it => it.Value))
        {
            if (_channels.ContainsKey(messageOption.Topic))
                continue;

            var channel = Channel.CreateBounded<IMessage>(
                                                          new BoundedChannelOptions(messageOption.GetMaxChannelSize(_options.Value))
                                                          {
                                                              SingleReader = true,
                                                              SingleWriter = true,
                                                              FullMode     = messageOption.GetBoundedChannelFullMode(_options.Value)
                                                          }
                                                         );

            _logger.LogDebug("Add channel for topic {ConfigTopic}", messageOption.Topic);

            var success = _channels.TryAdd(messageOption.Topic, channel);

            if (success)
                topics.Add(messageOption.Topic);
            else
            {
                _logger.LogWarning(
                                   "Add channel for topic {ConfigTopic} failed",
                                   messageOption.Topic
                                  );
            }
        }

        return topics;
    }

    public async ValueTask<bool> EnqueueAsync(
        IMessage          message,
        bool              useCompress       = true,
        bool              insertStorage     = true,
        CancellationToken cancellationToken = default
    )
    {
        _channels.TryGetValue(message.GetTopic()!, out var channel);

        if (channel is null)
        {
            _logger.LogWarning(
                               "message topic {Topic} not found matched channel, please check your consumer registration!",
                               message.GetTopic()
                              );

            return false;
        }

        if (
            useCompress
            && _consumerFactory.ConsumerOptions[message.GetTopic()!].GetEnableCompression(
                                                                                          _options.Value
                                                                                         )
        )
        {
            message.Body = _compressor!.Compress(message.Body);
        }

        if (
            _consumerFactory.ConsumerOptions[message.GetTopic()!].GetEnablePersistence(
                                                                                       _options.Value
                                                                                      ) && message.GetRetryCount() is null or 0
                                                                                        && insertStorage
        )
        {
            var opResult = await _persistStorage!.AddAsync(message);

            if (!opResult)
            {
                _logger.LogWarning(
                                   "message {MessageId} enqueue to persistent storage failed!",
                                   message.GetMessageId()
                                  );

                return false;
            }
        }

        // use WaitToWriteAsync internally to guarantee message will be write to channel
        await channel.Writer.WriteAsync(message, cancellationToken);

        return true;
    }

    public async ValueTask<bool> EnqueueAsync(
        IEnumerable<IMessage> messages,
        bool                  useCompress       = true,
        bool                  insertStorage     = true,
        CancellationToken     cancellationToken = default
    )
    {
        foreach (var message in messages)
        {
            if (!_channels.ContainsKey(message.GetTopic()!))
            {
                _logger.LogWarning(
                                   "message topic {Topic} not found matched channel, please check your consumer registration!",
                                   message.GetTopic()
                                  );

                return false;
            }

            if (
                useCompress
                && _consumerFactory.ConsumerOptions[message.GetTopic()!].GetEnableCompression(
                     _options.Value
                    )
            )
            {
                message.Body = _compressor!.Compress(message.Body);
            }
        }

        var persistMessages = messages.Where(it => it.GetRetryCount() is null or 0).ToList();

        if (_options.Value.EnablePersistence && persistMessages.Any() && insertStorage)
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
            await _channels[message.GetTopic()!].Writer.WriteAsync(message, cancellationToken);
        }

        return true;
    }

    public void Listen(CancellationToken cancellationToken)
    {
        foreach (var consumerOption in _consumerFactory.ConsumerOptions.Select(it => it.Value))
        {
            for (var i = 0; i < consumerOption.ParallelNum; i++)
            {
                Task.Factory.StartNew(
                                      async () => await PollingMessage(consumerOption, cancellationToken),
                                      cancellationToken,
                                      TaskCreationOptions.LongRunning,
                                      TaskScheduler.Default
                                     );
            }
        }
    }

    private async Task PollingMessage(
        MessageOptions    consumerOption,
        CancellationToken cancellationToken
    )
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _channels.TryGetValue(consumerOption.Topic, out var channel);

                while (channel!.Reader.CanPeek && channel.Reader.TryRead(out var message))
                {
                    await (
                              MessageReceivedEvent?.Invoke(message, consumerOption, cancellationToken)
                              ?? Task.CompletedTask
                          );
                }

                await Task.Delay(_options.Value.PollingInterval, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "poll and consume message error");
            }
        }
    }
}