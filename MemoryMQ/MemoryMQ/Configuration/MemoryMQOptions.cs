using System.Reflection;
using System.Threading.Channels;

namespace MemoryMQ.Configuration;

public class MemoryMQOptions
{
    /// <summary>
    /// 消费者所在的程序集
    /// </summary>
    public Assembly[] ConsumerAssemblies { get; set; } = { Assembly.GetEntryAssembly()! };
    
    /// <summary>
    /// 全局每条队列的最大队列长度
    /// </summary>
    public int GlobalMaxChannelSize { get; set; } = 10000;

    /// <summary>
    /// 队列满时的处理方式 默认等待
    /// </summary>
    public BoundedChannelFullMode GlobalBoundedChannelFullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// 拉取消息的间隔
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// 是否开启持久化，默认开启
    /// 开启后每条消息都会持久化到磁盘，能够保证消息至少消费一次，但是会影响性能
    /// </summary>
    public bool EnablePersistent { get; set; } = true;

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string DbConnectionString { get; set; } = "data source=memorymq.db";


    /// <summary>
    /// 全局重试次数 null或0表示不重试 
    /// </summary>
    public uint? GlobalRetryCount { get; set; } = 3; 
    
    /// <summary>
    /// 重试间隔
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 重试类型
    /// </summary>
    public RetryMode RetryMode { get; set; } = RetryMode.Fixed;
}