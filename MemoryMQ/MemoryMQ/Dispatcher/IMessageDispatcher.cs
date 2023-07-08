using MemoryMQ.Messages;

namespace MemoryMQ.Dispatcher;

public interface IMessageDispatcher
{
    /// <summary>
    /// 启动调度
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StartDispatchAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 停止调度
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StopDispatchAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 添加消息
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    ValueTask<bool> EnqueueAsync(IMessage message);
}