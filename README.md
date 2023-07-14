# <center> ![](icon.png)
# <center> MemoryMQ 

[中文](README_CN.md)|[English](README.md)

# Introduction

A memory-based(`System.Threading.Channels`) message queue library , primarily designed for simple standalone projects that do not want to introduce dependencies like RabbitMQ but still need a messaging queue with the following features:

1. Retry on failure (fixed interval, incremental interval, exponential interval)
2. Message persistence
3. Custom the concurrency, persistence, retry count, etc. of each consumer
4. Message compression

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

# Benchmark

Data Size: 53.2KB per message, message count: 100

```markdown
// * Summary *

BenchmarkDotNet v0.13.6, Windows 10 (10.0.19045.3208/22H2/2022Update)
AMD Ryzen 7 PRO 4750U with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 6.0.406
[Host]   : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2
.NET 6.0 : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2

Job=.NET 6.0  Runtime=.NET 6.0

|                               Method |       Mean |     Error |     StdDev |     Median |
|------------------------------------- |-----------:|----------:|-----------:|-----------:|
|                   PublishWithPersist | 104.733 ms | 3.2088 ms |  8.8917 ms | 106.186 ms |
|                PublishWithoutPersist |   3.145 ms | 0.3250 ms |  0.9584 ms |   3.937 ms |
| PublishWithoutPersistAndWithCompress |  36.384 ms | 3.9532 ms | 11.6560 ms |  30.872 ms |
|    PublishWithPersistAndWithCompress |  57.261 ms | 3.6179 ms | 10.4962 ms |  56.048 ms |
```