using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryMQ;

public class DispatcherService:IHostedService
{
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly ILogger<DispatcherService> _logger;

    public DispatcherService(IMessageDispatcher messageDispatcher,ILogger<DispatcherService> logger)
    {
        _messageDispatcher = messageDispatcher;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"MemoryMQ Dispatcher Service Started at {DateTime.Now}");
        return _messageDispatcher.StartDispatchAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"MemoryMQ Dispatcher Service Stoped at {DateTime.Now}");
        await _messageDispatcher.StopDispatchAsync(cancellationToken);
    }
}