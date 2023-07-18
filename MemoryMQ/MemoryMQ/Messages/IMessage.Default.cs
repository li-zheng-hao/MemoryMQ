namespace MemoryMQ.Messages;

public class Message : IMessage
{
    /// <summary>
    /// must assign IMessage.Body by your self
    /// </summary>
    public Message()
    {
        Header = new Dictionary<string, string>
        {
            { MessageHeader.MessageId, Guid.NewGuid().ToString() },
            { MessageHeader.Retry, "0" },
            { MessageHeader.CreatTime, DateTime.Now.Ticks.ToString() }
        };
        Body=string.Empty;
    }
    /// <summary>
    /// must assign IMessage.Body by your self
    /// </summary>
    /// <param name="topic"></param>
    public Message(string topic):this()
    {
        Header[MessageHeader.Topic] = topic;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="body">you should serialize your message by yourself, like System.Text.Json </param>
    public Message(string topic, string body):this(topic)
    {
        Body = body;
    }

    public Dictionary<string, string> Header { get; set; }

    public string Body { get; set; }
}