﻿using System.Data.SQLite;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Dispatcher;
using MemoryMQ.Publisher;
using MemoryMQ.RetryStrategy;
using MemoryMQ.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MemoryMQ;

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

        serviceCollection.AddSingleton<SQLiteConnection>(sp => 
            new SQLiteConnection(sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value.DbConnectionString)
                .OpenAndReturn());

        serviceCollection.AddSingleton<IMessagePublisher, MessagePublisher>();

        serviceCollection.AddSingleton<IRetryStrategy, DefaultRetryStrategy>();

        MemoryMQOptions memoryMqOptions = new MemoryMQOptions();

        config(memoryMqOptions);

        if (memoryMqOptions.EnablePersistent)
            serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        return serviceCollection;
    }
}