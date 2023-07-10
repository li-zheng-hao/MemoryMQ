# MemoryMQ

# 介绍

基于内存（`System.Threading.Channels`）的消息队列，主要适用的目标是一些非常简单的单体项目，不希望引入RabbitMQ等依赖的同时又希望有一个消息队列，支持功能：

1. 失败重试（固定间隔、递增间隔、指数间隔）
2. 消息持久化
3. 控制每个消费者的并发数

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

