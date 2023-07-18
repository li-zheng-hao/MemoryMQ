using MemoryMQ.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static void AddSqliteStorage(this MemoryMQOptions options)
    {
        options.Extensions.Add(new SqliteStorageExtension());
    }
}