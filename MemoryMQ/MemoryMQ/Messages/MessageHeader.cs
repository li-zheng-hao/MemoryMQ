namespace MemoryMQ.Messages;

/// <summary>
/// message header payloads
/// </summary>
public static class MessageHeader
{
    /// <summary>
    /// message id
    /// </summary>
    public const string MessageId = "message_id";

    /// <summary>
    /// topic
    /// </summary>
    public const string Topic = "topic";

    /// <summary>
    /// retry count
    /// </summary>
    public const string Retry = "retry";

    /// <summary>
    /// create time , DateTime.Now.Ticks
    /// </summary>
    public const string CreatTime = "create_time";
}

public static class MessageExtension
{
    public static string? GetMessageId(this IMessage message)
    {
        return message.Header.TryGetValue(MessageHeader.MessageId, out var messageId) ? messageId : null;
    }

    public static string? GetTopic(this IMessage message)
    {
        return message.Header.TryGetValue(MessageHeader.Topic, out var topic) ? topic : null;
    }

    public static string? GetCreateTime(this IMessage message)
    {
        return message.Header.TryGetValue(MessageHeader.CreatTime, out var createTime) ? createTime : null;
    }

    public static int? GetRetryCount(this IMessage message)
    {
        return message.Header.TryGetValue(MessageHeader.Retry, out var retry) ? Int32.Parse(retry) : null;
    }

    public static void IncreaseRetryCount(this IMessage message)
    {
        message.Header[MessageHeader.Retry] = (message.GetRetryCount() + 1 ?? 0 + 1).ToString();
    }
    
    public static void SetRetryCount(this IMessage message,long retryCount)
    {
        message.Header[MessageHeader.Retry] = retryCount.ToString();
    }
}