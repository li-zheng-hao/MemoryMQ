# <img src="icon.png" style="zoom: 33%;" />MemoryMQ

[中文](README_CN.md)|[English](README.md)

# 介绍

一个基于内存（`System.Threading.Channels`）的消息队列库，主要适用的目标是一些非常简单的单体项目，不希望引入RabbitMQ等依赖的同时又希望有一个消息队列，支持功能：

1. 失败重试（固定间隔、递增间隔、指数间隔）
2. 消息持久化
3. 控制每个消费者的并发数、持久化、重试次数等
4. 消息压缩

# 使用方式

支持.NET 6及以上项目，使用方式：

1. 引入依赖库

```shell
dotnet add package MemoryMQ
```

2. 注册服务及消费者

```c#
builder.Services.AddMemoryMQ(it =>
{
    // 是否开启持久化 目前仅支持Sqlite
    it.EnablePersistent = true;
    // 重试策略
    it.RetryMode = RetryMode.Incremental;
    // 重试间隔
    it.RetryInterval = TimeSpan.FromSeconds(5);
});

// 添加消费者,注意用Scoped生命周期
builder.Services.AddScoped<ConsumerA>();
builder.Services.AddScoped<ConsumerB>();
```

3. 配置消费者

```c#

// 实现IMessageConsumer接口
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

4. 发送消息

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


# 性能测试

数据大小：每条消息53.2KB,消息数量：100条

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
|                      PublishInMemory |   3.145 ms | 0.3250 ms |  0.9584 ms |   3.937 ms |
|                  PublishWithCompress |  36.384 ms | 3.9532 ms | 11.6560 ms |  30.872 ms |
|        PublishWithPersistAndCompress |  57.261 ms | 3.6179 ms | 10.4962 ms |  56.048 ms |
```