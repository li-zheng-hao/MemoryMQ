namespace MemoryMQ;

public interface IMessagePublisher
{
    ValueTask PublishAsync(IMessage message);

    ValueTask PublishAsync(string topic, string body);
}