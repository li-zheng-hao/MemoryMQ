using MemoryMQ.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static void AddLiteDBStorage(this MemoryMQOptions options)
    {
        options.Extensions.Add(new LiteDBStorageExtension());
    }
}