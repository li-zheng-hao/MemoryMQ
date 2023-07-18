using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Dispatcher;
using MemoryMQ.Publisher;
using MemoryMQ.RetryStrategy;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddMemoryMQ(this IServiceCollection serviceCollection,
        Action<MemoryMQOptions> config)
    {
        serviceCollection.Configure(config);

        serviceCollection.AddSingleton<IConsumerFactory, DefaultConsumerFactory>();

        serviceCollection.AddSingleton<IValidateOptions
            <MemoryMQOptions>, MemoryMQOptionsValidation>();

        serviceCollection.AddSingleton<IMessageDispatcher, DefaultMessageDispatcher>();

        serviceCollection.AddHostedService<DispatcherService>();

        serviceCollection.AddSingleton<IMessagePublisher, MessagePublisher>();

        serviceCollection.AddSingleton<IRetryStrategy, DefaultRetryStrategy>();

        MemoryMQOptions memoryMqOptions = new MemoryMQOptions();

        config(memoryMqOptions);

        if (memoryMqOptions.EnablePersistence)
        {
         
        }

        if (memoryMqOptions.EnableCompression)
        {
            serviceCollection.AddLZ4Compressor();
        }
        
        foreach (var extension in memoryMqOptions.Extensions)
        {
            extension.AddServices(serviceCollection);
        }

        return serviceCollection;
    }
}