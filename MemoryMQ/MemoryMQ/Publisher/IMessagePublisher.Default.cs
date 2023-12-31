﻿using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;

namespace MemoryMQ.Publisher;

/// <summary>
/// message publisher
/// </summary>
public class MessagePublisher : IMessagePublisher
{
    private readonly IMessageQueueManager _queueManager;

    public MessagePublisher(IMessageQueueManager queueManager)
    {
        _queueManager = queueManager;
    }

    public ValueTask<bool> PublishAsync(IMessage message)
    {
        ValidateMessage(message);

        return _queueManager.EnqueueAsync(message);
    }

    public ValueTask<bool> PublishAsync(string topic, string body)
    {
        var message = new Message(topic, body);

        return PublishAsync(message);
    }

    public ValueTask<bool> PublishAsync(ICollection<IMessage> messages)
    {
        foreach (var message in messages)
        {
            ValidateMessage(message);
        }

        return _queueManager.EnqueueAsync(messages);
    }

    private void ValidateMessage(IMessage message)
    {
        if (string.IsNullOrEmpty(message.GetTopic()))
            throw new ArgumentException("message topic should not be empty!");

        if (string.IsNullOrEmpty(message.GetMessageId()))
            throw new ArgumentException("message id should not be empty!");

        if (string.IsNullOrEmpty(message.GetCreateTime()))
            throw new ArgumentException("message create time should not be empty!");
    }
}
