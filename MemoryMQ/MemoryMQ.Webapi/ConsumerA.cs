namespace MemoryMQ.Webapi;

public class ConsumerA:IMessageConsumer
{
    private readonly ILogger<ConsumerA> _logger;

    public ConsumerA(ILogger<ConsumerA> logger)
    {
        _logger = logger;
    }
    public string Topic { get; set; } = "topic-a";
    public uint ParallelNum { get; set; } = 5;
    public Task ReceivedAsync(IMessage message)
    {
        _logger.LogInformation($"收到消息{message.Body} {DateTime.Now}");
        return Task.CompletedTask;
    }
}