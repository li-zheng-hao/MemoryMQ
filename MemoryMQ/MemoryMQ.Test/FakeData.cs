﻿using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Messages;

namespace MemoryMQ.Test;

public static class FakeData
{
    // public IMessageConsumer GetConsumer()
    // {
    //     return new FakeConsumer();
    // }
}

public class TestConsumer : IMessageConsumer
{
    public static int Received { get; set; } = 0;

    public static int ReceivedFailure { get; set; } = 0;
    public ConsumerOptions GetConsumerConfig()
    {
        return new ConsumerOptions()
        {
            Topic = "topic",
        };
    }

    public Task ReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        Received++;
        return Task.CompletedTask;
    }

    public Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken)
    {
        ReceivedFailure++;
        return Task.CompletedTask;
    }

}

public class TestSlowConsumer : IMessageConsumer
{
    public static int Received { get; set; } = 0;

    public static int ReceivedFailure { get; set; } = 0;
    public ConsumerOptions GetConsumerConfig()
    {
        return new ConsumerOptions()
        {
            Topic = "topic",
        };
    }

    public async Task ReceivedAsync(IMessage message, CancellationToken cancellationToken)
    {
        Received++;
        await Task.Delay(60_000, cancellationToken);
    }

    public async Task FailureRetryAsync(IMessage message, CancellationToken cancellationToken)
    {
        ReceivedFailure++;
        await Task.Delay(60_000, cancellationToken);
    }

}