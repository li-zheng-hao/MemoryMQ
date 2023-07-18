using System.Text.Json;
using MemoryMQ.Messages;

namespace MemoryMQ.Storage.LiteDB;

public class MemoryMQMessage
{
    public MemoryMQMessage()
    {
    }

    public MemoryMQMessage(IMessage message)
    {
        Message = JsonSerializer.Serialize(message);
        MessageId = message.GetMessageId();
        CreateTime = message.GetCreateTime();
        Retry = message.GetRetryCount() ?? 0;
    }
    
    public long Id { get; set; }

    public string Message { get; set; }

    public string MessageId { get; set; }

    public string CreateTime { get; set; }

    public int Retry { get; set; }
}