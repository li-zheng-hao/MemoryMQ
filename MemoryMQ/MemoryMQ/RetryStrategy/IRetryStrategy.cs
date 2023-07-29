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
    /// <param name="messageOptions">message option for this topic</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> ScheduleRetryAsync(
        IMessage          message,
        MessageOptions    messageOptions,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// message retry exceeding the retry count event
    /// </summary>
    Func<IMessage, Task>? MessageRetryFailureEvent { get; set; }
}