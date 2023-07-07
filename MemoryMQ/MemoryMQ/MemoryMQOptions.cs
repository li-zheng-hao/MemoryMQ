using System.Threading.Channels;

namespace MemoryMQ;

public class MemoryMQOptions
{
    
    /// <summary>
    /// 全局每条队列的最大队列长度
    /// </summary>
    public int GlobalMaxChannelSize { get; set; }=10000;
    
    /// <summary>
    /// 队列满时的处理方式 默认等待
    /// </summary>
    public BoundedChannelFullMode GlobalBoundedChannelFullMode { get; set; } = BoundedChannelFullMode.Wait;
    
    /// <summary>
    /// 拉取消息的间隔
    /// </summary>
    public TimeSpan PollingInterval { get; set; }=TimeSpan.FromMilliseconds(500);
    
    /// <summary>
    /// 是否开启持久化，默认开启
    /// 开启后每条消息都会持久化到磁盘，能够保证消息至少消费一次，但是会影响性能
    /// </summary>
    public bool EnablePersistent { get; set; } = true;
    
}