using System.Diagnostics;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;
using MemoryMQ.RetryStrategy;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryMQ.Test;

public class RetryStrategyTest
{
    
    
    [Fact]
    public async Task MessageDontRetry_Passed()
    {
        bool retryTrigger = false;
        bool retryFailureTrigger = false;

        var serviceCollection = new ServiceCollection();

        serviceCollection.Configure<MemoryMQOptions>(config =>
        {
            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistence = false;
            config.GlobalRetryCount =0;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<DefaultRetryStrategy>();
        var sp = serviceCollection.BuildServiceProvider();
        var strategy = sp.GetService<DefaultRetryStrategy>();

        IMessage msg = new Message("topic", "hello");
        
        strategy.MessageRetryEvent = async message =>
        {
            retryTrigger = true;

            await Task.CompletedTask;
        };
        strategy.MessageRetryFailureEvent = async message =>
        {
            retryFailureTrigger = true;

            await Task.CompletedTask;
        };

        await strategy.ScheduleRetryAsync(msg, (new TestConsumer()).GetConsumerConfig(), default);

        await Task.Delay(1000);
        
        Assert.False(retryTrigger);
        Assert.False(retryFailureTrigger);

    }
    
    [Fact]
    public async Task MessageRetryFailure_Passed()
    {
        bool retryTrigger = false;
        bool retryFailureTrigger = false;

        var serviceCollection = new ServiceCollection();

        serviceCollection.Configure<MemoryMQOptions>(config =>
        {
            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistence = false;
            config.GlobalRetryCount = 1;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<DefaultRetryStrategy>();
        var sp = serviceCollection.BuildServiceProvider();
        var strategy = sp.GetService<DefaultRetryStrategy>();

        IMessage msg = new Message("topic", "hello");
        
        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        
        strategy.MessageRetryEvent = async message =>
        {
            retryTrigger = true;

            await Task.CompletedTask;
        };
        strategy.MessageRetryFailureEvent = async message =>
        {
            retryFailureTrigger = true;

            await Task.CompletedTask;
        };

        await strategy.ScheduleRetryAsync(msg, (new TestConsumer()).GetConsumerConfig(), default);

        await AssertEx.WaitUntil(() => retryFailureTrigger, 1000,6000);
        Assert.False(retryTrigger);

    }
    [Fact]
    public async Task MessageRetry_Passed()
    {
        bool retryTrigger = false;

        var serviceCollection = new ServiceCollection();

        serviceCollection.Configure<MemoryMQOptions>(config =>
        {
            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnablePersistence = false;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<DefaultRetryStrategy>();
        await using var sp = serviceCollection.BuildServiceProvider();
        var strategy = sp.GetService<DefaultRetryStrategy>();

        IMessage msg = new Message("topic", "hello");

        strategy.MessageRetryEvent = async message =>
        {
            retryTrigger = true;

            await Task.CompletedTask;
        };

        await strategy.ScheduleRetryAsync(msg, (new TestConsumer()).GetConsumerConfig(), default);

        await AssertEx.WaitUntil(() => retryTrigger, 1000,6000);
    }


    [Fact]
    public async Task MessageRetryIncremental_Passed()
    {
        var retryTrigger = false;
        var serviceCollection = new ServiceCollection();

        serviceCollection.Configure<MemoryMQOptions>(config =>
        {
            config.RetryInterval = TimeSpan.FromSeconds(1);
            config.EnablePersistence = false;
            config.RetryMode = RetryMode.Incremental;
            config.GlobalRetryCount = 10;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<DefaultRetryStrategy>();
        await using var sp = serviceCollection.BuildServiceProvider();
        var strategy = sp.GetService<DefaultRetryStrategy>();

        IMessage msg = new Message("topic", "hello");
        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        Stopwatch sw = new Stopwatch();
        sw.Start();

        strategy.MessageRetryEvent =  message =>
        {
            retryTrigger = true;

            return Task.CompletedTask;
        };

        await strategy.ScheduleRetryAsync(msg, (new TestConsumer()).GetConsumerConfig(), default);

        var cancellationTokenSource = new CancellationTokenSource(10000);

        while (!retryTrigger && !cancellationTokenSource.IsCancellationRequested)
        {
            await Task.Delay(500, cancellationTokenSource.Token);
        }
        Assert.True(sw.ElapsedMilliseconds > 2000);

        Assert.True(retryTrigger);
    }

    [Fact]
    public async Task MessageRetryFixed_Passed()
    {
        var retryTrigger = false;
        var serviceCollection = new ServiceCollection();

        serviceCollection.Configure<MemoryMQOptions>(config =>
        {
            config.RetryInterval = TimeSpan.FromSeconds(1);
            config.EnablePersistence = false;
            config.RetryMode = RetryMode.Fixed;
            config.GlobalRetryCount = 10;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<DefaultRetryStrategy>();
        await using var sp = serviceCollection.BuildServiceProvider();
        var strategy = sp.GetService<DefaultRetryStrategy>();

        IMessage msg = new Message("topic", "hello");
        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        Stopwatch sw = new Stopwatch();
        sw.Start();

        strategy.MessageRetryEvent = async message =>
        {
            retryTrigger = true;

            await Task.CompletedTask;
        };

        await strategy.ScheduleRetryAsync(msg, (new TestConsumer()).GetConsumerConfig(), default);

        var cancellationTokenSource = new CancellationTokenSource(10000);

        while (!retryTrigger && !cancellationTokenSource.IsCancellationRequested)
        {
            await Task.Delay(500, cancellationTokenSource.Token);
        }

        Assert.True(retryTrigger);
    }


    [Fact]
    public async Task MessageRetryExponential_Passed()
    {
        var retryTrigger = false;
        var serviceCollection = new ServiceCollection();

        serviceCollection.Configure<MemoryMQOptions>(config =>
        {
            config.RetryInterval = TimeSpan.FromSeconds(1);
            config.EnablePersistence = false;
            config.RetryMode = RetryMode.Exponential;
            config.GlobalRetryCount = 10;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<DefaultRetryStrategy>();
        await using var sp = serviceCollection.BuildServiceProvider();
        var strategy = sp.GetService<DefaultRetryStrategy>();

        IMessage msg = new Message("topic", "hello");
        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        Stopwatch sw = new Stopwatch();
        sw.Start();

        strategy.MessageRetryEvent = message =>
        {
            retryTrigger = true;

            return Task.CompletedTask;
        };

        await strategy.ScheduleRetryAsync(msg, (new TestConsumer()).GetConsumerConfig(), default);

        var cancellationTokenSource = new CancellationTokenSource(10000);

        while (!retryTrigger && !cancellationTokenSource.IsCancellationRequested)
        {
            await Task.Delay(500, cancellationTokenSource.Token);
        }

        Assert.True(sw.ElapsedMilliseconds > 4000);

        Assert.True(retryTrigger);
    }
}

