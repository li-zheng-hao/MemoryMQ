using System.Reflection;
using System.Text.Json;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;
using MemoryMQ.Publisher;
using MemoryMQ.RetryStrategy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Newtonsoft.Json;

namespace MemoryMQ.Test;

public class PublishTest
{
    private IMessage _message = new Message("topic", "body");

    private ICollection<IMessage> _messages = new List<IMessage>()
                                              {
                                                  new Message("topic", "body"),
                                                  new Message("topic", "body")
                                              };

    [Fact]
    public async Task PublishOneMessage()
    {
        Mock<IMessageQueueManager> dispatcherMock = new();

        dispatcherMock
            .Setup(x => x.EnqueueAsync(It.IsAny<IMessage>(), true, true, default).Result)
            .Returns(true);

        MessagePublisher messagePublisher = new(dispatcherMock.Object);

        var res = await messagePublisher.PublishAsync(_message);

        Assert.True(res);

        dispatcherMock.Verify(
                              it => it.EnqueueAsync(It.IsAny<IMessage>(), true, true, default),
                              Times.Once
                             );

        var res2 = await messagePublisher.PublishAsync("topic", "body");

        Assert.True(res2);

        dispatcherMock.Verify(
                              it => it.EnqueueAsync(It.IsAny<IMessage>(), true, true, default),
                              Times.Exactly(2)
                             );
    }

    [Fact]
    public async Task PublishMultipleMessage()
    {
        Mock<IMessageQueueManager> dispatcherMock = new();

        dispatcherMock
            .Setup(x => x.EnqueueAsync(It.IsAny<ICollection<IMessage>>(), true, true, default).Result)
            .Returns(true);

        MessagePublisher messagePublisher = new(dispatcherMock.Object);
        var              res              = await messagePublisher.PublishAsync(_messages);
        Assert.True(res);

        dispatcherMock.Verify(
                              it => it.EnqueueAsync(It.IsAny<ICollection<IMessage>>(), true, true, default),
                              Times.Once
                             );
    }
}