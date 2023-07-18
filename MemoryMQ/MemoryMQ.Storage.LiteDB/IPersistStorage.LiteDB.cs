using LiteDB;
using MemoryMQ.Messages;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MemoryMQ.Storage.LiteDB;

public class LiteDBPersistStorage : IPersistStorage
{
    private readonly ILogger<LiteDBPersistStorage> _logger;

    private readonly LiteDatabase _database;

    private readonly ILiteCollection<MemoryMQMessage> _queue;

    public LiteDBPersistStorage(ILogger<LiteDBPersistStorage> logger, LiteDatabase database)
    {
        _logger = logger;
        _database = database;
        _queue = _database.GetCollection<MemoryMQMessage>();
    }

    public Task CreateTableAsync()
    {
        _queue.EnsureIndex(it => it.MessageId);

        return Task.CompletedTask;
    }

    public Task<bool> UpdateRetryAsync(IMessage message)
    {
        var msg = _queue.FindOne(it => it.MessageId == message.GetMessageId());

        msg.Retry = (int)message.GetRetryCount();

        var opResult = _queue.Update(msg);

        return Task.FromResult(opResult);
    }

    public Task<bool> AddAsync(IMessage message)
    {
        var msg = new MemoryMQMessage(message);
        var id = _queue.Insert(msg);

        return Task.FromResult(id != null);
    }

    public Task<bool> RemoveAsync(IMessage message)
    {
        var msg = _queue.FindOne(it => it.MessageId == message.GetMessageId());
        var opResult = _queue.Delete(msg.Id);

        return Task.FromResult(opResult);
    }

    public Task<IEnumerable<IMessage>> RestoreAsync()
    {
        var msgs = _queue.FindAll();
        var messages = new List<IMessage>();

        foreach (var memoryMQMessage in msgs)
        {
            var msg = JsonSerializer.Deserialize<Message>(memoryMQMessage.Message);
            messages.Add(msg);
        }

        return Task.FromResult<IEnumerable<IMessage>>(messages);
    }

    public Task<bool> AddAsync(ICollection<IMessage> message)
    {
        var msgs = message.Select(it => new MemoryMQMessage(it));
        
        var opResult = _queue.InsertBulk(msgs);

        return Task.FromResult(opResult == message.Count);
    }
}