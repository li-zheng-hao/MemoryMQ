using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;

namespace MemoryMQ.Benchmark;

public class TestConsumer : IMessageConsumer
{
    public MessageOptions GetMessageConfig()
    {
        return new MessageOptions()
        {
            Topic = "topic"
        };
    }

    public Task ReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}