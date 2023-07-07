using System.Text.Json;

namespace MemoryMQ;

public class Message : IMessage
{
    public Message(string topic, string body)
    {
        Header = new Dictionary<string, string>
        {
            { MessageHeader.MessageId, Guid.NewGuid().ToString() },
            { MessageHeader.Topic, topic }
        };
        
        Body = body;
    }
    public Dictionary<string, string> Header { get; set; }

    public string Body { get; set; }
}

public  static class MessageExtension
{
    public static string? GetMessageId(this IMessage message)
    {
        return message.Header.TryGetValue(MessageHeader.MessageId, out var messageId) ? messageId : null;
    }
    
    public static string? GetTopic(this IMessage message)
    {
        return message.Header.TryGetValue(MessageHeader.Topic, out var topic) ? topic : null;
    }
}