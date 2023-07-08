using MemoryMQ.Configuration;

namespace MemoryMQ.Consumer;

public interface IConsumerFactory
{
    Dictionary<string, Type> Consumers { get; init; }

    Dictionary<string,MessageOptions> ConsumerOptions { get; init; }
    IMessageConsumer? CreateConsumer(IServiceProvider serviceProvider,string topic);
}