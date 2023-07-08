namespace MemoryMQ;

public class MessageOptions
{
    /// <summary>
    /// 订阅主题
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// 同时处理消息的数量
    /// </summary>
    public uint ParallelNum { get; set; } 
    
    /// <summary>
    /// 重试次数 null表示使用全局配置 0表示不需要重试
    /// </summary>
    public uint? RetryCount { get; set; } 
    
}