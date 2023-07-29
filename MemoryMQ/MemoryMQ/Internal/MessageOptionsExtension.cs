using System.Threading.Channels;
using MemoryMQ.Configuration;

namespace MemoryMQ.Internal;

/// <summary>
/// extensions for MessageOptions
/// </summary>
internal static class MessageOptionsExtension
{
    public static bool GetEnablePersistence(this MessageOptions options, MemoryMQOptions globalOptions)
    {
        return options.EnablePersistence ?? globalOptions.EnablePersistence;
    }

    public static uint GetRetryCount(this MessageOptions options, MemoryMQOptions globalOptions)
    {
        return options.RetryCount ?? globalOptions.GlobalRetryCount ?? 0;
    }

    public static BoundedChannelFullMode GetBoundedChannelFullMode(this MessageOptions options, MemoryMQOptions globalOptions)
    {
        return options.BoundedChannelFullMode ?? globalOptions.GlobalBoundedChannelFullMode;
    }

    public static int GetMaxChannelSize(this MessageOptions options, MemoryMQOptions globalOptions)
    {
        return options.MaxChannelSize ?? globalOptions.GlobalMaxChannelSize;
    }

    public static bool GetEnableCompression(this MessageOptions options, MemoryMQOptions globalOptions)
    {
        return options.EnableCompression ?? globalOptions.EnableCompression;
    }
}