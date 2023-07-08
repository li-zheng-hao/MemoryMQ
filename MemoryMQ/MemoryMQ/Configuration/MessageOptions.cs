namespace MemoryMQ.Configuration;

public class MessageOptions
{
    /// <summary>
    /// 订阅主题
    /// </summary>
    public string Topic { get; init; } = null!;

    /// <summary>
    /// 同时处理消息的数量
    /// </summary>
    public uint ParallelNum { get; init; } = 1;
    
    /// <summary>
    /// 重试次数 null表示使用全局配置 0表示不需要重试
    /// </summary>
    public uint? RetryCount { get; init; } 
    
}