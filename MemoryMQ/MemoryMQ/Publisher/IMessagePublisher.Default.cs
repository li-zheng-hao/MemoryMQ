using Microsoft.Extensions.Options;

namespace MemoryMQ;

public class MessagePublisher:IMessagePublisher
{
    private readonly IMessageDispatcher _dispatcher;
    private readonly IPersistStorage? _persistStorage;
    private readonly IOptions<MemoryMQOptions> _options;

    public MessagePublisher(IMessageDispatcher dispatcher,IOptions<MemoryMQOptions> options,IPersistStorage? persistStorage=null)
    {
        _dispatcher = dispatcher;
        _persistStorage = persistStorage;
        _options = options;
    }
    public async ValueTask PublishAsync(IMessage message)
    {
       await _dispatcher.EnqueueAsync(message);
    }
    
    public async ValueTask PublishAsync(string topic,string body)
    {
        var message = new Message(topic,body);
        await PublishAsync(message);
    }
}