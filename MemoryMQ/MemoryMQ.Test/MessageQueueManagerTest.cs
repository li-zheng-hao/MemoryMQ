using EasyCompressor;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;
using MemoryMQ.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MemoryMQ.Test;

public class MessageQueueManagerTest
{
    [Fact]
    public void InitConsumers()
    {
        var loggerMock          = new Mock<ILogger<DefaultMessageQueueManager>>();
        var consumerFactoryMock = new Mock<IConsumerFactory>();

        consumerFactoryMock
            .Setup(x => x.ConsumerOptions)
            .Returns(
                     new Dictionary<string, MessageOptions>()
                     {
                         {
                             "topic", new MessageOptions()
                                      {
                                          Topic = "topic"
                                      }
                         }
                     }
                    );

        IOptions<MemoryMQOptions> options            = Options.Create(new MemoryMQOptions());
        var                       compressorMock     = new Mock<ICompressor>();
        var                       persistStorageMock = new Mock<IPersistStorage>();

        DefaultMessageQueueManager manager =
            new(
                loggerMock.Object,
                consumerFactoryMock.Object,
                options,
                compressorMock.Object,
                persistStorageMock.Object
               );

        var consumers = manager.InitConsumers();

        Assert.Single(consumers);
    }

    [Fact]
    public async Task Enqueue()
    {
        var loggerMock          = new Mock<ILogger<DefaultMessageQueueManager>>();
        var consumerFactoryMock = new Mock<IConsumerFactory>();

        consumerFactoryMock
            .Setup(x => x.ConsumerOptions)
            .Returns(
                     new Dictionary<string, MessageOptions>()
                     {
                         {
                             "topic", new MessageOptions()
                                      {
                                          Topic = "topic"
                                      }
                         }
                     }
                    );

        IOptions<MemoryMQOptions> options            = Options.Create(new MemoryMQOptions());
        var                       compressorMock     = new Mock<ICompressor>();
        var                       persistStorageMock = new Mock<IPersistStorage>();

        persistStorageMock.Setup(it => it.AddAsync(It.IsAny<IMessage>()).Result).Returns(true);

        DefaultMessageQueueManager manager =
            new(
                loggerMock.Object,
                consumerFactoryMock.Object,
                options,
                compressorMock.Object,
                persistStorageMock.Object
               );

        var msg = new Message("topic", "body");

        manager.InitConsumers();

        var opResult = await manager.EnqueueAsync(msg);

        Assert.True(opResult);

        persistStorageMock.Verify(it => it.AddAsync(It.IsAny<IMessage>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueMultiMessages()
    {
        var loggerMock          = new Mock<ILogger<DefaultMessageQueueManager>>();
        var consumerFactoryMock = new Mock<IConsumerFactory>();

        consumerFactoryMock
            .Setup(x => x.ConsumerOptions)
            .Returns(
                     new Dictionary<string, MessageOptions>()
                     {
                         {
                             "topic", new MessageOptions()
                                      {
                                          Topic = "topic"
                                      }
                         }
                     }
                    );

        IOptions<MemoryMQOptions> options            = Options.Create(new MemoryMQOptions());
        var                       compressorMock     = new Mock<ICompressor>();
        var                       persistStorageMock = new Mock<IPersistStorage>();

        persistStorageMock.Setup(it => it.AddAsync(It.IsAny<ICollection<IMessage>>()).Result).Returns(true);

        DefaultMessageQueueManager manager =
            new(
                loggerMock.Object,
                consumerFactoryMock.Object,
                options,
                compressorMock.Object,
                persistStorageMock.Object
               );

        var            msg  = new Message("topic", "body");
        var            msg2 = new Message("topic", "body");
        List<IMessage> msgs = new List<IMessage>() { msg2, msg };

        manager.InitConsumers();

        var opResult = await manager.EnqueueAsync(msgs);

        Assert.True(opResult);

        persistStorageMock.Verify(it => it.AddAsync(It.IsAny<ICollection<IMessage>>()), Times.Once);
    }

    [Fact]
    public async Task ReceivedMessage()
    {
        var loggerMock          = new Mock<ILogger<DefaultMessageQueueManager>>();
        var consumerFactoryMock = new Mock<IConsumerFactory>();

        consumerFactoryMock
            .Setup(x => x.ConsumerOptions)
            .Returns(
                     new Dictionary<string, MessageOptions>()
                     {
                         {
                             "topic", new MessageOptions()
                                      {
                                          Topic = "topic"
                                      }
                         }
                     }
                    );

        IOptions<MemoryMQOptions> options = Options.Create(new MemoryMQOptions()
                                                           {
                                                               EnableCompression = false
                                                           });

        var compressorMock     = new Mock<ICompressor>();
        var persistStorageMock = new Mock<IPersistStorage>();

        persistStorageMock.Setup(it => it.AddAsync(It.IsAny<IMessage>()).Result).Returns(true);

        DefaultMessageQueueManager manager =
            new(
                loggerMock.Object,
                consumerFactoryMock.Object,
                options,
                compressorMock.Object,
                persistStorageMock.Object
               );

        var msg = new Message("topic", "body");

        manager.InitConsumers();

        manager.Listen(default);

        var opResult = await manager.EnqueueAsync(msg);


        Assert.True(opResult);

        Message?       receivedMessage        = default!;
        MessageOptions receivedMessageOptions = default!;

        manager.MessageReceivedEvent = (message, messageOptions, cancellationToken) =>
                                       {
                                           receivedMessage        = message as Message;
                                           receivedMessageOptions = messageOptions;

                                           return Task.CompletedTask;
                                       };


        await Task.Delay(1000);

        Assert.NotNull(receivedMessage);
        Assert.NotNull(receivedMessageOptions);

        Assert.Equal("topic", receivedMessage!.Header[MessageHeader.Topic]);
        Assert.Equal("body",  receivedMessage.Body);
        Assert.Equal(0,       receivedMessage.GetRetryCount());
    }
}