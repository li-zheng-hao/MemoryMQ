using System.Reflection;
using System.Text.Json;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;
using MemoryMQ.Publisher;
using MemoryMQ.RetryStrategy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace MemoryMQ.Test;

public class PublishTest
{
    private readonly ServiceProvider _provider;

    [Fact]
    public void Serialize()
    {
        var str = JsonConvert.SerializeObject(new
        {
            a = 1,
            b = 2,
            c = new object[]
            {
                "a", false
            }
        });
    }

    [Fact]
    public async Task PublishNoConsumer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.Configure<MemoryMQOptions>(config =>
        {
            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistent = false;
        });

        services.AddSingleton<MessagePublisher>();
        services.AddSingleton<IMessageDispatcher, DefaultMessageDispatcher>();
        services.AddSingleton<IRetryStrategy, DefaultRetryStrategy>();
        services.AddSingleton<IConsumerFactory, DefaultConsumerFactory>();
        var provider = services.BuildServiceProvider();

        var publisher = provider.GetService<MessagePublisher>();

        var res = await publisher.PublishAsync("topic", "message");

        Assert.False(res);
    }

    [Fact]
    public async Task Publish_Passed()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddMemoryMQ(it =>
        {
            it.EnablePersistent = false;

            it.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };
        });

        services.AddScoped<TestConsumer>();

        var provider = services.BuildServiceProvider();
        var dispatcherService = provider.GetService<IHostedService>();
        await dispatcherService.StartAsync(default);

        var publisher = provider.GetService<IMessagePublisher>();

        var res = await publisher.PublishAsync("topic", "message");

        Assert.True(res);
    }
}