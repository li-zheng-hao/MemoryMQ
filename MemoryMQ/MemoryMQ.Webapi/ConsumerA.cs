namespace MemoryMQ.Webapi;

public class ConsumerA : IMessageConsumer
{
    private readonly ILogger<ConsumerA> _logger;

    public ConsumerA(ILogger<ConsumerA> logger)
    {
        _logger = logger;
        
    }

    public MessageOptions Config { get; } = new MessageOptions()
    {
        Topic = "topic-a",
        ParallelNum = 5,
        RetryCount = 0
    };

    public Task ReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"收到消息{message.Body} {DateTime.Now}");
        return Task.CompletedTask;
    }

    public Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}