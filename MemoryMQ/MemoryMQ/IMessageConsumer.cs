namespace MemoryMQ;

public interface IMessageConsumer
{
    /// <summary>
    /// 订阅主题
    /// </summary>
    string Topic { get; set; }

    /// <summary>
    /// 同时处理消息的数量
    /// </summary>
    uint ParallelNum { get; set; } 
    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task ReceivedAsync(IMessage message);
}