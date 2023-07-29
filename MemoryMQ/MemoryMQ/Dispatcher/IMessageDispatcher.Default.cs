using EasyCompressor;
using MemoryMQ.Compress;
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

    private readonly IMessageQueueManager _queueManager;

    private CancellationToken _cancelToken;

    public DefaultMessageDispatcher(
        ILogger<DefaultMessageDispatcher> logger,
        IOptions<MemoryMQOptions>         options,
        IServiceProvider                  serviceProvider,
        IRetryStrategy                    retryStrategy,
        IMessageQueueManager              queueManager,
        IPersistStorage?                  persistStorage = null
    )
    {
        _logger                                = logger;
        _options                               = options;
        _serviceProvider                       = serviceProvider;
        _queueManager                          = queueManager;
        _persistStorage                        = persistStorage;
        retryStrategy.MessageRetryFailureEvent = RetryMessageFailureAsync;
    }

    public Task StopDispatchAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StartDispatchAsync(CancellationToken cancellationToken)
    {
        _cancelToken = cancellationToken;

        if (_options.Value.EnablePersistence && _persistStorage is not null)
            await _persistStorage.CreateTableAsync();

        _queueManager.InitConsumers();

        _queueManager.MessageReceivedEvent += ConsumeMessage;

        _queueManager.Listen(cancellationToken);

        if (_options.Value.EnablePersistence)
            await RestoreMessagesAsync(cancellationToken);
    }

    private async Task ConsumeMessage(
        IMessage          message,
        MessageOptions    messageOptions,
        CancellationToken cancellationToken
    )
    {
        using var scope = _serviceProvider.CreateScope();

        var consumerExecutor = scope.ServiceProvider.GetService<IConsumerExecutor>();

        if (consumerExecutor is null) _logger.LogWarning("can not find consumer for {MessageOptionsTopic}", messageOptions.Topic);

        await consumerExecutor!.ConsumeMessage(message, cancellationToken);
    }

    private async Task RestoreMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var messages = await _persistStorage!.RestoreAsync();

            await _queueManager.EnqueueAsync(messages, false, false, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "restore messages error");
        }
    }

    private async Task RetryMessageFailureAsync(IMessage message)
    {
        using var scope = _serviceProvider.CreateScope();


        var consumerExecutor = scope.ServiceProvider.GetService<IConsumerExecutor>();

        if (consumerExecutor is null) _logger.LogWarning("can not find consumer for {MessageOptionsTopic}", message.GetTopic());

        await consumerExecutor!.ConsumeFailureAsync(message, _cancelToken);
    }
}