using MemoryMQ.Dispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryMQ.Test;

public class ServiceCollectionExtensionTest
{
    [Fact]
    public void AddMemoryMQ()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddMemoryMQ(it => { });

        var serviceProvider = services.BuildServiceProvider();

        var manager = serviceProvider.GetService<IMessageQueueManager>();

        Assert.NotNull(manager);
    }
}