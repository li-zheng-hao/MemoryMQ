using MemoryMQ.Messages;

namespace MemoryMQ.Publisher;

public interface IMessagePublisher
{
    ValueTask<bool> PublishAsync(IMessage message);

    ValueTask<bool> PublishAsync(string topic, string body);
}