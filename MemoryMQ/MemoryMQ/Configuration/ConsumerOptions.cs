using System.Threading.Channels;

namespace MemoryMQ.Configuration;

/// <summary>
/// consumer options
/// </summary>
public class ConsumerOptions
{
    /// <summary>
    /// message topic, required and unique
    /// </summary>
    public string Topic { get; init; } = null!;

    /// <summary>
    /// parallel num, default is 1
    /// </summary>
    public uint ParallelNum { get; init; } = 1;
    
    /// <summary>
    /// retry count, if null will use global setting, if 0 means no retry
    /// </summary>
    public uint? RetryCount { get; init; } 
    
    /// <summary>
    /// Enable Persistence (only support sqlite), default is null (will use global setting)
    /// </summary>
    public bool? EnablePersistence { get; set; }
    
    /// <summary>
    /// behavior when channel is full, default is null (will use global setting)
    /// </summary>
    public BoundedChannelFullMode? BoundedChannelFullMode { get; set; }
    
    /// <summary>
    /// max channel size (default is null, will use global setting)
    /// </summary>
    public int? MaxChannelSize { get; set; }
    
    /// <summary>
    /// enable compression, default is null (will use global setting)
    /// </summary>
    public bool? EnableCompression { get; set; }
    
    
}