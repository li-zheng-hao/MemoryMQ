using MemoryMQ.Consumer;
using MemoryMQ.Messages;

namespace MemoryMQ.RetryStrategy;

public interface IRetryStrategy
{
    /// <summary>
    /// 计划重试
    /// </summary>
    /// <param name="message"></param>
    /// <param name="consumer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ScheduleRetry(IMessage message, IMessageConsumer consumer,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// 消息重试触发事件
    /// </summary>
    Func<IMessage,Task> MessageRetryEvent { get; set; }
}