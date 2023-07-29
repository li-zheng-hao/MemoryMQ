using System.Diagnostics;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;
using MemoryMQ.RetryStrategy;
using MemoryMQ.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MemoryMQ.Test;

public class RetryStrategyTest
{
    [Fact]
    public async Task ScheduleRetryToDeadLetterQueue()
    {
        var mockLogger = new Mock<ILogger<DefaultRetryStrategy>>();

        var options = Options.Create(
            new MemoryMQOptions()
            {
                EnablePersistence = true,
                EnableDeadLetterQueue = true,
                GlobalRetryCount = 1
            }
        );
        Mock<IMessageQueueManager> mockMessageQueueManager = new();
        Mock<IPersistStorage> mockStorage = new();

        mockStorage
            .Setup(x => x.AddToDeadLetterQueueAsync(It.IsAny<IMessage>()).Result)
            .Returns(true);

        mockStorage.Setup(x => x.RemoveAsync(It.IsAny<IMessage>()).Result).Returns(true);

        DefaultRetryStrategy mockStrategy = new DefaultRetryStrategy(
            mockLogger.Object,
            options,
            mockMessageQueueManager.Object,
            mockStorage.Object
        );

        var msg = new Message("topic", "hello");

        msg.SetRetryCount(2);

        await mockStrategy.ScheduleRetryAsync(
            msg,
            new MessageOptions() { Topic = "topic", RetryCount = 1 },
            default
        );

        mockStorage.Verify(x => x.AddToDeadLetterQueueAsync(It.IsAny<IMessage>()), Times.Once);
    }

    [Fact]
    public async Task MessageDontRetry_Passed()
    {
        var mockLogger = new Mock<ILogger<DefaultRetryStrategy>>();

        var options = Options.Create(
            new MemoryMQOptions()
            {
                EnablePersistence = true,
                EnableDeadLetterQueue = true,
                GlobalRetryCount = 1
            }
        );

        Mock<IMessageQueueManager> mockMessageQueueManager = new();

        Mock<IPersistStorage> mockStorage = new();

        mockStorage.Setup(x => x.RemoveAsync(It.IsAny<IMessage>()).Result).Returns(true);

        DefaultRetryStrategy mockStrategy = new DefaultRetryStrategy(
            mockLogger.Object,
            options,
            mockMessageQueueManager.Object,
            mockStorage.Object
        );

        var msg = new Message("topic", "hello");

        var messageOptions = new MessageOptions() { Topic = "topic", RetryCount = 0 };

        await mockStrategy.ScheduleRetryAsync(msg, messageOptions, default);

        mockStorage.Verify(x => x.RemoveAsync(It.IsAny<IMessage>()), Times.Once);
    }

    [Fact]
    public async Task MessageRetryFailureEnqueue()
    {
        var mockLogger = new Mock<ILogger<DefaultRetryStrategy>>();

        var options = Options.Create(
            new MemoryMQOptions()
            {
                EnablePersistence = true,
                EnableDeadLetterQueue = true,
                GlobalRetryCount = 1
            }
        );
        Mock<IMessageQueueManager> mockMessageQueueManager = new();

        Mock<IPersistStorage> mockStorage = new();

        mockStorage.Setup(x => x.UpdateRetryAsync(It.IsAny<IMessage>()).Result).Returns(true);

        var mockStrategy = new DefaultRetryStrategy(
            mockLogger.Object,
            options,
            mockMessageQueueManager.Object,
            mockStorage.Object
        );

        var msg = new Message("topic", "hello");

        var messageOptions = new MessageOptions() { Topic = "topic", RetryCount = 2 };

        var opResult = await mockStrategy.ScheduleRetryAsync(msg, messageOptions, default);

        mockStorage.Verify(x => x.UpdateRetryAsync(It.IsAny<IMessage>()), Times.Once);

        Assert.True(opResult);
    }
}
