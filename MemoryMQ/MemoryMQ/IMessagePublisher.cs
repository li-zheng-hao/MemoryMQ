namespace MemoryMQ;

public interface IMessagePublisher
{
    ValueTask Publish(IMessage message);

    ValueTask Publish(string topic, string body);
}