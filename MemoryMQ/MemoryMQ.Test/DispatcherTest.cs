using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Dispatcher;
using MemoryMQ.RetryStrategy;
using MemoryMQ.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MemoryMQ.Test;

public class DispatcherTest
{
    [Fact]
    public async Task StartDispatchTest()
    {
        // mock
        Mock<ILogger<DefaultMessageDispatcher>> logger = new();

        IOptions<MemoryMQOptions> options = Options.Create(
                                                           new MemoryMQOptions() { EnablePersistence = true }
                                                          );

        Mock<IServiceProvider> serviceProvider = new();
        Mock<IConsumerFactory> consumerFactory = new();

        consumerFactory
            .Setup(it => it.ConsumerOptions)
            .Returns(
                     new Dictionary<string, MessageOptions>()
                     {
                         {
                             "topic",
                             new MessageOptions() { Topic = "topic" }
                         }
                     }
                    );

        Mock<IRetryStrategy>       retryStrategy  = new();
        Mock<IMessageQueueManager> queueManager   = new();
        Mock<IPersistStorage>      persistStorage = new();

        DefaultMessageDispatcher messageDispatcher = new DefaultMessageDispatcher(
                                                                                  logger.Object,
                                                                                  options,
                                                                                  serviceProvider.Object,
                                                                                  retryStrategy.Object,
                                                                                  queueManager.Object,
                                                                                  persistStorage.Object
                                                                                 );

        await messageDispatcher.StartDispatchAsync(default);
    }
}