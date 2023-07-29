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
}
