namespace MemoryMQ.Messages;

public class Message : IMessage
{
    public Message()
    {
        Header = new Dictionary<string, string>();
        Body   = string.Empty;
    }

    public Message(string topic, string body)
    {
        Header = new Dictionary<string, string>
                 {
                     { MessageHeader.MessageId, Guid.NewGuid().ToString() },
                     { MessageHeader.Topic, topic },
                     { MessageHeader.Retry, "0" },
                     { MessageHeader.CreatTime, DateTime.Now.Ticks.ToString() }
                 };

        Body = body;
    }

    public Dictionary<string, string> Header { get; set; }

    public string Body { get; set; }
}