using System.Data.SQLite;
using MemoryMQ.Configuration;
using MemoryMQ.ServiceExtensions;
using MemoryMQ.Storage;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class SqliteStorageExtension:IExtension
{
    public void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<SQLiteConnection>(sp => 
            new SQLiteConnection(sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value.DbConnectionString)
                .OpenAndReturn());
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();
    }
}