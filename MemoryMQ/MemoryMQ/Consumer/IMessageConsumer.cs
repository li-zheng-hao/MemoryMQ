using MemoryMQ.Configuration;
using MemoryMQ.Messages;

namespace MemoryMQ.Consumer;

public interface IMessageConsumer
{
    MessageOptions Config { get; }

    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ReceivedAsync(IMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// 超过重试次数
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken);
}