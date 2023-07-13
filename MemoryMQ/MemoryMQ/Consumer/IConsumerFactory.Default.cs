using MemoryMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Consumer;

public class DefaultConsumerFactory : IConsumerFactory
{
    private readonly ILogger<DefaultConsumerFactory> _logger;

    private readonly IOptions<MemoryMQOptions> _options;

    public Dictionary<string, Type> Consumers { get; init; }

    public Dictionary<string, MessageOptions> ConsumerOptions { get; init; }

    public DefaultConsumerFactory(ILogger<DefaultConsumerFactory> logger,IServiceProvider serviceProvider, IOptions<MemoryMQOptions> options)
    {
        _logger = logger;
        _options = options;
        Consumers = new Dictionary<string, Type>();
        ConsumerOptions = new();
        InitConsumers(serviceProvider);
    }

    private void InitConsumers(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        foreach (var consumerAssembly in _options.Value.ConsumerAssemblies)
        {
            var consumerTypes = consumerAssembly.GetExportedTypes()
                .Where(it => it.IsAssignableTo(typeof(IMessageConsumer))).ToList();
            foreach (var consumerType in consumerTypes)
            {
                if (scope.ServiceProvider.GetService(consumerType) is not IMessageConsumer consumer) 
                    continue;
             
                Consumers.Add(consumer.GetMessageConfig().Topic, consumer.GetType());
                ConsumerOptions.Add(consumer.GetMessageConfig().Topic, consumer.GetMessageConfig());
            }
        }
        
        if(!Consumers.Any())
            _logger.LogWarning("no consumer found!");
            
    }

    public virtual IMessageConsumer? CreateConsumer(IServiceProvider serviceProvider, string topic)
    {
        Consumers.TryGetValue(topic, out var type);

        if (type != null)
        {
            var service = serviceProvider.GetService(type);
            return service as IMessageConsumer;
        }

        return null;
    }
}