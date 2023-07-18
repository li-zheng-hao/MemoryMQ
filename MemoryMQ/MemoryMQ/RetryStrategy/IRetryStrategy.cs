using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;

namespace MemoryMQ.RetryStrategy;

public interface IRetryStrategy
{
    /// <summary>
    /// schedule message retry
    /// </summary>
    /// <param name="message"></param>
    /// <param name="consumerOptions">message option for this topic</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ScheduleRetryAsync(IMessage message, ConsumerOptions consumerOptions, CancellationToken cancellationToken);

    /// <summary>
    /// message retry event
    /// </summary>
    Func<IMessage, Task> MessageRetryEvent { get; set; }

    /// <summary>
    /// message retry exceeding the retry count event
    /// </summary>
    Func<IMessage, Task> MessageRetryFailureEvent { get; set; }
}