using MemoryMQ.Messages;

namespace MemoryMQ.Test;

public class MessageHeaderTest
{
    [Fact]
    public void GetTopic_Passed()
    {
        IMessage msg = new Message("topic", "hello");
        Assert.Equal("topic",msg.GetTopic());
    }
    
    [Fact]
    public void GetMessageId_Passed()
    {
        IMessage msg = new Message("topic", "hello");
        msg.Header[MessageHeader.MessageId] = "msgid";
        Assert.Equal("msgid",msg.GetMessageId());
    }
    
    [Fact]
    public void IncreaseRetryCount_Passed()
    {
        IMessage msg = new Message("topic", "hello");
        msg.IncreaseRetryCount();
        Assert.Equal(1,msg.GetRetryCount());
    }
    
    [Fact]
    public void GetCreateTime_Passed()
    {
        var ticks=DateTime.Now.Ticks.ToString();
        IMessage msg = new Message("topic", "hello");
        msg.Header[MessageHeader.CreatTime]=ticks;
        Assert.Equal(ticks,msg.GetCreateTime());
    }
}