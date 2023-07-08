using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MemoryMQ;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddMemoryMQ(this IServiceCollection serviceCollection,
        Action<MemoryMQOptions> config)
    {

        serviceCollection.Configure(config);
        
        serviceCollection.AddSingleton<IValidateOptions
            <MemoryMQOptions>, MemoryMQOptionsValidation>();

        serviceCollection.AddSingleton<IMessageDispatcher, DefaultMessageDispatcher>();

        serviceCollection.AddHostedService<DispatcherService>();

        serviceCollection.AddSingleton<IMessagePublisher, MessagePublisher>();

        MemoryMQOptions memoryMqOptions = new MemoryMQOptions();

        config(memoryMqOptions);

        if (memoryMqOptions.EnablePersistent)
            serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        return serviceCollection;
    }

}