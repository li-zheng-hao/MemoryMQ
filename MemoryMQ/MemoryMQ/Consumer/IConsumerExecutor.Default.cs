using MemoryMQ.Configuration;
using MemoryMQ.Messages;
using MemoryMQ.RetryStrategy;
using MemoryMQ.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Consumer;

public class ConsumerExecutor : IConsumerExecutor
{
    private readonly ILogger<ConsumerExecutor> _logger;

    private readonly IOptions<MemoryMQOptions> _options;

    private readonly IPersistStorage _persistStorage;

    private readonly IConsumerFactory _consumerFactory;

    private readonly IServiceProvider _serviceProvider;

    private readonly IRetryStrategy _retryStrategy;

    public ConsumerExecutor(ILogger<ConsumerExecutor> logger,
                            IOptions<MemoryMQOptions> options,
                            IPersistStorage           persistStorage,
                            IConsumerFactory          consumerFactory,
                            IServiceProvider          serviceProvider,
                            IRetryStrategy            retryStrategy)
    {
        _logger          = logger;
        _options         = options;
        _persistStorage  = persistStorage;
        _consumerFactory = consumerFactory;
        _serviceProvider = serviceProvider;
        _retryStrategy   = retryStrategy;
    }

    public async Task ConsumeMessage(IMessage message, CancellationToken cancellationToken)
    {
        var consumer = _consumerFactory.CreateConsumer(_serviceProvider, message.GetTopic()!);

        if (consumer is null)
        {
            _logger.LogWarning("{MessageOptionsTopic} consumer is null", message.GetTopic()!);

            return;
        }

        bool isConsumeSuccess = true;

        try
        {
            _logger.LogDebug($"consume message {message.GetMessageId()} at {DateTime.Now}");
            
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
                if (_options.Value.EnablePersistence)
                    await _persistStorage
                        !.RemoveAsync(message);
            }
            else
            {
                message.IncreaseRetryCount();

                await _retryStrategy.ScheduleRetryAsync(
                                                        message,
                                                        consumer.GetMessageConfig(),
                                                        cancellationToken
                                                       );
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "retry message error {MessageOptionsTopic}", message.GetTopic());
        }
    }

    public async Task ConsumeFailureAsync(IMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var consumer = _consumerFactory.CreateConsumer(_serviceProvider, message.GetTopic()!);

            if (consumer is null)
            {
                _logger.LogWarning("{MessageOptionsTopic} consumer is null", message.GetTopic()!);

                return;
            }

            await consumer.FailureRetryAsync(message, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "consumer received message error");
        }
    }
}