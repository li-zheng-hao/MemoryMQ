using System.Text.Json;

namespace MemoryMQ;

public class Message : IMessage
{
    public Message()
    {
    }

    public Message(string topic, string body)
    {
        Header = new Dictionary<string, string>
        {
            { MessageHeader.MessageId, Guid.NewGuid().ToString() },
            { MessageHeader.Topic, topic },
            { MessageHeader.Retry, "0" },
            { MessageHeader.CreatTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
        };
        
        Body = body;
    }

    public Dictionary<string, string> Header { get; set; }

    public string Body { get; set; }
}