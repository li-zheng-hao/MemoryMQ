using MemoryMQ.Messages;

namespace MemoryMQ.Dispatcher;

public interface IMessageDispatcher
{
    /// <summary>
    /// start dispatch service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StartDispatchAsync(CancellationToken cancellationToken);

    /// <summary>
    /// stop dispatch service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StopDispatchAsync(CancellationToken cancellationToken);

    /// <summary>
    /// add one message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    ValueTask<bool> EnqueueAsync(IMessage message);
    
    /// <summary>
    /// add multiple messages
    /// </summary>
    /// <param name="messages"></param>
    /// <returns></returns>
    ValueTask<bool> EnqueueAsync(IEnumerable<IMessage> messages);
}