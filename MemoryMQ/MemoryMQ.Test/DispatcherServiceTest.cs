using MemoryMQ.Dispatcher;
using Microsoft.Extensions.Logging;
using Moq;

namespace MemoryMQ.Test;

public class DispatcherServiceTest
{
    Mock<IMessageDispatcher> messageDispatcher = new();

    Mock<ILogger<DispatcherService>> logger = new();

    [Fact]
    public async Task Start()
    {
        var dispatcherService = new DispatcherService(messageDispatcher.Object, logger.Object);
        await dispatcherService.StartAsync(default);
    }

    [Fact]
    public async Task Stop()
    {
        var dispatcherService = new DispatcherService(messageDispatcher.Object, logger.Object);
        await dispatcherService.StopAsync(default);
    }
}