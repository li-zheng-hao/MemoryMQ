using Microsoft.Extensions.DependencyInjection;

namespace MemoryMQ;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddMemoryMQ(this IServiceCollection serviceCollection,Action<MemoryMQOptions> config)
    {
        serviceCollection.Configure(config);
        
        serviceCollection.AddSingleton<IMessageDispatcher, DefaultMessageDispatcher>();
        
        serviceCollection.AddHostedService<DispatcherService>();
        
        serviceCollection.AddSingleton<IMessagePublisher,MessagePublisher>();
        
        serviceCollection.AddSingleton<IPersistStorage,PersistStorage>();
        
        
        return serviceCollection;
    }
}