using MemoryMQ.Configuration;
using MemoryMQ.Messages;

namespace MemoryMQ.Consumer;

public interface IMessageConsumer
{
    /// <summary>
    /// 获取消费者配置
    /// </summary>
    /// <returns></returns>
    MessageOptions GetMessageConfig(); 

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