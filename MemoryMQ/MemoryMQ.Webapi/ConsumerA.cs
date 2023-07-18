using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;

namespace MemoryMQ.Webapi;

public class ConsumerA : IMessageConsumer,IDisposable
{
    private readonly ILogger<ConsumerA> _logger;

    public ConsumerA(ILogger<ConsumerA> logger)
    {
        _logger = logger;
        _logger.LogInformation("ConsumerA init at {Now}", DateTime.Now);
    }

    public ConsumerOptions GetConsumerConfig()
    {
        return new ConsumerOptions()
        {
            Topic = "topic-a",
            ParallelNum = 5,
            RetryCount = 3
        };
    }

    public Task ReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("received {MessageBody} {Now}", message.Body, DateTime.Now);
        throw new Exception("test expection");
        return Task.CompletedTask;
    }

    public Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("retry max times {RetryTimes} {MessageBody} {Now}",message.GetRetryCount(), message.Body, DateTime.Now);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _logger.LogInformation("ConsumerA disposed at {Now}", DateTime.Now);
    }
}