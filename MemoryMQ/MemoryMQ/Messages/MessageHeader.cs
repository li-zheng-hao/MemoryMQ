namespace MemoryMQ.Messages;

public class MessageHeader
{
    /// <summary>
    /// 消息id
    /// </summary>
    public const string MessageId = "message_id";

    /// <summary>
    /// 主题
    /// </summary>
    public const string Topic = "topic";

    /// <summary>
    /// 重试次数
    /// </summary>
    public const string Retry = "retry";

    /// <summary>
    /// 创建时间 从1970-01-01 00:00:00到现在的UTC秒数
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
}