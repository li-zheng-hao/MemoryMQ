using MemoryMQ.Configuration;
using MemoryMQ.Messages;

namespace MemoryMQ.Consumer;

public interface IMessageConsumer
{
    /// <summary>
    /// get consumer option
    /// </summary>
    /// <returns></returns>
    ConsumerOptions GetConsumerConfig(); 

    /// <summary>
    /// received message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ReceivedAsync(IMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// retry message failure
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken);
}