using LiteDB;
using MemoryMQ.Configuration;
using MemoryMQ.ServiceExtensions;
using MemoryMQ.Storage;
using MemoryMQ.Storage.LiteDB;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class LiteDBStorageExtension:IExtension
{
    public void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<LiteDatabase>(sp => 
            new LiteDatabase(sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value.DbConnectionString));
        serviceCollection.AddSingleton<IPersistStorage, LiteDBPersistStorage>();
    }
}