using MemoryMQ.Messages;

namespace MemoryMQ.Consumer;

public interface IConsumerExecutor
{
    Task ConsumeMessage(IMessage message, CancellationToken cancellationToken);

    Task ConsumeFailureAsync(IMessage message, CancellationToken cancellationToken);
}