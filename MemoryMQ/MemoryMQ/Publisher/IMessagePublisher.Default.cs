using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;

namespace MemoryMQ.Publisher;

public class MessagePublisher : IMessagePublisher
{
    private readonly IMessageDispatcher _dispatcher;

    public MessagePublisher(IMessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public ValueTask<bool> PublishAsync(IMessage message)
    {
        return _dispatcher.EnqueueAsync(message);
    }

    public ValueTask<bool> PublishAsync(string topic, string body)
    {
        var message = new Message(topic, body);
        
        return PublishAsync(message);
    }
}