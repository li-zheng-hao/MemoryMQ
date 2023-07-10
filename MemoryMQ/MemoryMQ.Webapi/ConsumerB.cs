using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;

namespace MemoryMQ.Webapi;

public class ConsumerB : IMessageConsumer
{
    private readonly ILogger<ConsumerB> _logger;

    public ConsumerB(ILogger<ConsumerB> logger)
    {
        _logger = logger;
        
        logger.LogInformation("ConsumerB init");
    }

    public MessageOptions GetMessageConfig()
    {
        return new MessageOptions()
        {
            Topic = "topic-b",
            ParallelNum = 1,
            RetryCount = 3
        };
    }

    public Task ReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("received {MessageBody} {Now}", message.Body, DateTime.Now);
        // throw new Exception("ex");
        return Task.CompletedTask;
    }

    public Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("retry max times {MessageBody} {Now}", message.Body, DateTime.Now);
        return Task.CompletedTask;
    }
    
}