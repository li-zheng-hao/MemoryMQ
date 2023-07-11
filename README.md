# MemoryMQ

[中文](README_CN.md)|[English](README.md)

# Introduction

A memory-based(`System.Threading.Channels`) message queue library , primarily designed for simple standalone projects that do not want to introduce dependencies like RabbitMQ but still need a messaging queue with the following features:

1. Retry on failure (fixed interval, incremental interval, exponential interval)
2. Message persistence
3. Control over the concurrency of each consumer

# Usage

Supports .NET 6 and above projects. Usage:

1. add nuget library

```shell
dotnet add package MemoryMQ
```

2. register services and consumers

```c#
builder.Services.AddMemoryMQ(it =>
{
    // support persistent, only Sqlite
    it.EnablePersistent = true;
    
    it.RetryMode = RetryMode.Incremental;
    
    it.RetryInterval = TimeSpan.FromSeconds(5);
});

// add consumers, use Scoped lifetime
builder.Services.AddScoped<ConsumerA>();
builder.Services.AddScoped<ConsumerB>();
```

3. configure consumers

```c#

// implement IMessageConsumer interface
public class ConsumerA : IMessageConsumer
{
    private readonly ILogger<ConsumerA> _logger;

    public ConsumerA(ILogger<ConsumerA> logger)
    {
        _logger = logger;
    }

    public MessageOptions GetMessageConfig()
    {
        return new MessageOptions()
        {
            Topic = "topic-a",
            ParallelNum = 5,
            RetryCount = 3
        };
    }

    public Task ReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("received {MessageBody} {Now}", message.Body, DateTime.Now);
        return Task.CompletedTask;
    }

    public Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("retry max times {RetryTimes} {MessageBody} {Now}",message.GetRetryCount(), message.Body, DateTime.Now);
        return Task.CompletedTask;
    }
}
```

4. send message

```c#

var publisher=serviceProvider.GetService<IMessagePublisher>();

var message = new Message()
{
    Body = "hello world",
    Topic = "topic-a"
};
await publisher.SendAsync(message);

// or this way
await publisher.SendAsync("topic-a","hello world");
```

