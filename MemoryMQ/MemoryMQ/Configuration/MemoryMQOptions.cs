using System.Reflection;
using System.Threading.Channels;

namespace MemoryMQ.Configuration;

/// <summary>
/// MemoryMQ options
/// </summary>
public class MemoryMQOptions
{
    /// <summary>
    /// consumer assemblies
    /// </summary>
    public Assembly[] ConsumerAssemblies { get; set; } = { Assembly.GetEntryAssembly()! };

    /// <summary>
    /// global channel max size
    /// </summary>
    public int GlobalMaxChannelSize { get; set; } = 10000;

    /// <summary>
    /// behavior when channel is full, default is wait
    /// </summary>
    public BoundedChannelFullMode GlobalBoundedChannelFullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// enable message compression, default is true
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// interval to poll message from queue
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// enable persistent (only support sqlite), default is true
    /// </summary>
    public bool EnablePersistence { get; set; } = true;

    /// <summary>
    /// database connection string (now only support sqlite), default is 'data source=memorymq.db'
    /// </summary>
    public string DbConnectionString { get; set; } = "data source=memorymq.db";


    /// <summary>
    /// global retry count, null or 0 means no retry
    /// </summary>
    public uint? GlobalRetryCount { get; set; } = 3;

    /// <summary>
    /// retry interval, default is 10 seconds
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// retry mode, default is fixed
    /// <see cref="RetryMode"/>
    /// </summary>
    public RetryMode RetryMode { get; set; } = RetryMode.Fixed;

    /// <summary>
    /// Gets or sets a value indicating whether to use a dead letter queue for messages that cannot be consumed.
    /// default is false.
    /// you can find failure messages from database table 'memorymq_deadmessage'
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = false;
}