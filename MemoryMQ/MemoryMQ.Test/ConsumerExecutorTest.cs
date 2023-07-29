using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;
using MemoryMQ.RetryStrategy;
using MemoryMQ.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MemoryMQ.Test;

public class ConsumerExecutorTest
{
    [Fact]
    public async Task ConsumeMessage()
    {
        var loggerMock = new Mock<ILogger<ConsumerExecutor>>();

        IOptions<MemoryMQOptions> options = Options.Create(new MemoryMQOptions()
                                                           {
                                                               EnablePersistence = true
                                                           });

        var persistStorageMock  = new Mock<IPersistStorage>();
        var consumerFactoryMock = new Mock<IConsumerFactory>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var retryStrategyMock   = new Mock<IRetryStrategy>();

        Mock<IMessageConsumer> messageConsumerMock = new();

        consumerFactoryMock.Setup(x => x.CreateConsumer(It.IsAny<IServiceProvider>(), "topic"))
                           .Returns(messageConsumerMock.Object);

        ConsumerExecutor consumerExecutor = new(loggerMock.Object, options, persistStorageMock.Object,
                                                consumerFactoryMock.Object,
                                                serviceProviderMock.Object, retryStrategyMock.Object);

        messageConsumerMock.Setup(it => it.ReceivedAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>()));
        messageConsumerMock.Setup(it => it.FailureRetryAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>()));

        await consumerExecutor.ConsumeMessage(new Message("topic", "body"), default);

        messageConsumerMock.Verify(it => it.ReceivedAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>())
                                   , Times.Once);
    }


    [Fact]
    public async Task ConsumeMessageThrowException()
    {
        var loggerMock = new Mock<ILogger<ConsumerExecutor>>();

        IOptions<MemoryMQOptions> options = Options.Create(new MemoryMQOptions()
                                                           {
                                                               EnablePersistence = true
                                                           });

        var persistStorageMock  = new Mock<IPersistStorage>();
        var consumerFactoryMock = new Mock<IConsumerFactory>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var retryStrategyMock   = new Mock<IRetryStrategy>();

        Mock<IMessageConsumer> messageConsumerMock = new();

        consumerFactoryMock.Setup(x => x.CreateConsumer(It.IsAny<IServiceProvider>(), "topic"))
                           .Returns(messageConsumerMock.Object);

        ConsumerExecutor consumerExecutor = new(loggerMock.Object, options, persistStorageMock.Object,
                                                consumerFactoryMock.Object,
                                                serviceProviderMock.Object, retryStrategyMock.Object);

        messageConsumerMock.Setup(it => it.ReceivedAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>()))
                           .Throws(new Exception("custom exception"));

        messageConsumerMock.Setup(it => it.FailureRetryAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>()));

        await consumerExecutor.ConsumeMessage(new Message("topic", "body"), default);

        messageConsumerMock.Verify(it => it.ReceivedAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>())
                                   , Times.Once);

        retryStrategyMock
            .Verify(it => it.ScheduleRetryAsync(It.IsAny<IMessage>(), It.IsAny<MessageOptions>()
                                                , It.IsAny<CancellationToken>()),
                    Times.Once);
    }

    [Fact]
    public async Task ConsumeFailureNotify()
    {
        var loggerMock = new Mock<ILogger<ConsumerExecutor>>();

        IOptions<MemoryMQOptions> options = Options.Create(new MemoryMQOptions()
                                                           {
                                                               EnablePersistence = true
                                                           });

        var persistStorageMock  = new Mock<IPersistStorage>();
        var consumerFactoryMock = new Mock<IConsumerFactory>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var retryStrategyMock   = new Mock<IRetryStrategy>();

        Mock<IMessageConsumer> messageConsumerMock = new();

        consumerFactoryMock.Setup(x => x.CreateConsumer(It.IsAny<IServiceProvider>(), "topic"))
                           .Returns(messageConsumerMock.Object);

        ConsumerExecutor consumerExecutor = new(loggerMock.Object, options, persistStorageMock.Object,
                                                consumerFactoryMock.Object,
                                                serviceProviderMock.Object, retryStrategyMock.Object);

        messageConsumerMock.Setup(it => it.ReceivedAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>()));
        messageConsumerMock.Setup(it => it.FailureRetryAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>()));

        await consumerExecutor.ConsumeFailureAsync(new Message("topic", "body"), default);

        messageConsumerMock.Verify(it => it.FailureRetryAsync(It.IsAny<IMessage>(), It.IsAny<CancellationToken>())
                                   , Times.Once);
    }
}