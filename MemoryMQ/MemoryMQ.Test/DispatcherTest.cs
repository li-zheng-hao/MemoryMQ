using System.Data.SQLite;
using System.Reflection;
using MemoryMQ.Configuration;
using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryMQ.Test;

public class DispatcherTest : IDisposable
{
    private readonly string _dbName;

    private readonly string _dbConnectionString;

    private readonly string _dbpath;

    public DispatcherTest()
    {
        _dbName = $"{Guid.NewGuid().ToString()}.db";
        _dbConnectionString = $"Data Source={_dbName};Pooling=false";
        _dbpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dbName);
    }

    [Fact]
    public async Task TestFullQueue_Passed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistent = false;
            config.DbConnectionString = _dbConnectionString;
        });

        await using var sp = services.BuildServiceProvider();
    }
    
    [Fact]
    public async Task TestEnqueueArgsCheck_Passed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistent = true;
            config.DbConnectionString = _dbConnectionString;
        });

        await using var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetService<IMessageDispatcher>() as DefaultMessageDispatcher;
        dispatcher.StartDispatchAsync(default);
        var msg=new Message("topic", "hello");
        msg.Header[MessageHeader.MessageId] = null;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await dispatcher.EnqueueAsync(msg);
        });
        
        var msg2=new Message("topic", "hello");
        msg2.Header[MessageHeader.Topic] = null;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await dispatcher.EnqueueAsync(msg2);
        });
        
        var msg3=new Message("topic", "hello");
        msg3.Header[MessageHeader.CreatTime] = null;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await dispatcher.EnqueueAsync(msg3);
        });
    }
    
    [Fact]
    public async Task TestEnqueueNoConsumer_Passed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistent = true;
            config.DbConnectionString = _dbConnectionString;
        });

        await using var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetService<IMessageDispatcher>() as DefaultMessageDispatcher;
        dispatcher.StartDispatchAsync(default);
        var opResult = await dispatcher.EnqueueAsync(new Message("topic", "hello"));
        Assert.False(opResult);
    }
    [Fact]
    public async Task TestEnqueue_Passed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistent = true;
            config.DbConnectionString = _dbConnectionString;
        });

        services.AddScoped<TestConsumer>();
        await using var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetService<IMessageDispatcher>() as DefaultMessageDispatcher;
        dispatcher.StartDispatchAsync(default);
        var opResult = await dispatcher.EnqueueAsync(new Message("topic", "hello"));
        Assert.True(opResult);
    }

    [Fact]
    public async Task TestRetry_Passed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistent = true;
            config.DbConnectionString = _dbConnectionString;
        });

        services.AddScoped<TestConsumer>();
        await using var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetService<IMessageDispatcher>() as DefaultMessageDispatcher;
        dispatcher.StartDispatchAsync(default);
        await dispatcher.RetryMessageAsync(new Message("topic", "body"));

        AssertEx.WaitUntil(() => TestConsumer.Received == 1);
    }

    [Fact]
    public async Task TestRetryFailure_Passed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistent = true;
            config.GlobalRetryCount = 1;
            config.DbConnectionString = _dbConnectionString;
        });

        services.AddScoped<TestConsumer>();
        await using var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetService<IMessageDispatcher>() as DefaultMessageDispatcher;
        dispatcher.StartDispatchAsync(default);
        var msg = new Message("topic", "body");
        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        await dispatcher.RetryMessageFailureAsync(msg);

        AssertEx.WaitUntil(() => TestConsumer.ReceivedFailure == 1);
        AssertEx.WaitUntil(() => TestConsumer.Received == 0);
    }

    public void Dispose()
    {
        if (File.Exists(_dbpath))
            File.Delete(_dbpath);
    }
}