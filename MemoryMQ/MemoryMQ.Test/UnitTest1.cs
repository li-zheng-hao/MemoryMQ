using System.Text.Json;
using MemoryMQ.Messages;

namespace MemoryMQ.Test;

public class UnitTest1
{
    [Fact]
    public void SerializeTest()
    {
        Message msg=new Message("test","test");
        var msgStr=JsonSerializer.Serialize(msg);
        var msg2=JsonSerializer.Deserialize<Message>(msgStr);
        Assert.True(msg2!.Body==msg.Body);
        Assert.True(msg2.GetMessageId()==msg.GetMessageId());

    }
}