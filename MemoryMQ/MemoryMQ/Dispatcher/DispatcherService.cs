using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryMQ.Dispatcher;

public class DispatcherService : IHostedService
{
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly ILogger<DispatcherService> _logger;

    public DispatcherService(IMessageDispatcher messageDispatcher, ILogger<DispatcherService> logger)
    {
        _messageDispatcher = messageDispatcher;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("MemoryMQ Dispatcher Service Started at {Now}", DateTime.Now);
            return _messageDispatcher.StartDispatchAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "MemoryMQ Dispatcher Service Start Failed");
            return Task.CompletedTask;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MemoryMQ Dispatcher Service Stoped at {Now}", DateTime.Now);
        await _messageDispatcher.StopDispatchAsync(cancellationToken);
    }
}