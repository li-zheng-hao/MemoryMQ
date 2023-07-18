using LiteDB;
using MemoryMQ.Configuration;
using MemoryMQ.Messages;
using MemoryMQ.Storage;
using MemoryMQ.Storage.LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryMQ.Test;

public class LiteDBTest
{
    private readonly string _dbPath;

    public LiteDBTest()
    {
        _dbPath = Path.Combine(AppContext.BaseDirectory, "memorymq_litedb.db");

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task AddMessage()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbPath; });
        LiteDBStorageExtension liteDBStorageExtension = new();
        liteDBStorageExtension.AddServices(serviceCollection);
        await using var sp = serviceCollection.BuildServiceProvider();

        var storage = sp.GetService<IPersistStorage>();
        var inserted = await storage.AddAsync(new Message("topic", "hello"));
        Assert.True(inserted);

        var db = sp.GetService<LiteDatabase>();
        var collection = db.GetCollection<MemoryMQMessage>();
        Assert.Equal(1, collection.Count());
    }
    
    [Fact]
    public async Task UpdateMessage()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbPath; });
        LiteDBStorageExtension liteDBStorageExtension = new();
        liteDBStorageExtension.AddServices(serviceCollection);
        await using var sp = serviceCollection.BuildServiceProvider();

        var storage = sp.GetService<IPersistStorage>();
        var msg=new Message("topic", "hello");
        await storage.AddAsync(msg);
        msg.IncreaseRetryCount();
        var opResult= await storage.UpdateRetryAsync(msg);

        Assert.True(opResult);
        
        var db = sp.GetService<LiteDatabase>();
        var collection = db.GetCollection<MemoryMQMessage>();
        var mqMessage = collection.FindOne(it => it.MessageId == msg.GetMessageId());
        Assert.Equal(1, mqMessage.Retry);
    }
    
    [Fact]
    public async Task RemoveMessage()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbPath; });
        LiteDBStorageExtension liteDBStorageExtension = new();
        liteDBStorageExtension.AddServices(serviceCollection);
        await using var sp = serviceCollection.BuildServiceProvider();

        var storage = sp.GetService<IPersistStorage>();
        var msg=new Message("topic", "hello");
         await storage.AddAsync(msg);
         var opResult =await storage.RemoveAsync(msg);
        Assert.True(opResult);
        
        var db = sp.GetService<LiteDatabase>();
        var collection = db.GetCollection<MemoryMQMessage>();
        Assert.Equal(0, collection.Count());

    }
    
    [Fact]
    public async Task RestoreMessage()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbPath; });
        LiteDBStorageExtension liteDBStorageExtension = new();
        liteDBStorageExtension.AddServices(serviceCollection);
        await using var sp = serviceCollection.BuildServiceProvider();

        var storage = sp.GetService<IPersistStorage>();
        var msg=new Message("topic", "hello");
        await storage.AddAsync(msg);
        var msgs=await storage.RestoreAsync();
        Assert.Equal(1,msgs.Count());
        Assert.Equal(msg.GetMessageId(),msgs.First().GetMessageId());

    }
    
    [Fact]
    public async Task AddBatchMessages()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbPath; });
        LiteDBStorageExtension liteDBStorageExtension = new();
        liteDBStorageExtension.AddServices(serviceCollection);
        await using var sp = serviceCollection.BuildServiceProvider();

        var storage = sp.GetService<IPersistStorage>();
        List<IMessage> msgs = new();
        for (int i = 0; i < 10; i++)
        {
            var msg=new Message("topic", "hello");
            msgs.Add(msg);
        }
        
        await storage.AddAsync(msgs);
        
        var db = sp.GetService<LiteDatabase>();
        var collection = db.GetCollection<MemoryMQMessage>();
        Assert.Equal(msgs.Count(), collection.Count());

    }
}