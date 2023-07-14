namespace MemoryMQ.Messages;

public interface IMessage
{
    Dictionary<string, string> Header { get; }
    
    string Body { get; set; }
}