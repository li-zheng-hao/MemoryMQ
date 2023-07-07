using Microsoft.Extensions.Options;

namespace MemoryMQ;

public class MessagePublisher:IMessagePublisher
{
    private readonly IMessageDispatcher _dispatcher;
    private readonly IPersistStorage _persistStorage;
    private readonly IOptions<MemoryMQOptions> _options;

    public MessagePublisher(IMessageDispatcher dispatcher,IPersistStorage persistStorage,IOptions<MemoryMQOptions> options)
    {
        _dispatcher = dispatcher;
        _persistStorage = persistStorage;
        _options = options;
    }
    public async ValueTask Publish(IMessage message)
    {
        if (_options.Value.EnablePersistent)
            await _persistStorage.AddAsync(message);
        
        _dispatcher.Enqueue(message);
    }
    
    public async ValueTask Publish(string topic,string body)
    {
        var message = new Message(topic,body);
        await Publish(message);
    }
}