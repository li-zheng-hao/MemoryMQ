namespace MemoryMQ;

public interface IMessage
{
    Dictionary<string,string> Header { get; set; }
    
    string Body { get; set; }
}