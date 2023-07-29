using System.Data;
using System.Data.SQLite;
using MemoryMQ.Configuration;
using MemoryMQ.Messages;
using MemoryMQ.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Test;

public class SqliteStorageTest : IDisposable
{
    private readonly string _dbpath;

    private readonly string _dbName;

    private readonly string _dbConnectionString;

    public SqliteStorageTest()
    {
        _dbName             = $"{Guid.NewGuid().ToString()}.db";
        _dbConnectionString = $"Data Source={_dbName};Pooling=false";
        _dbpath             = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dbName);
    }

    [Fact]
    public async Task AddToDeadLetterQueueAsync_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it =>
                                                     {
                                                         it.DbConnectionString    = _dbConnectionString;
                                                         it.EnableDeadLetterQueue = true;
                                                     });

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var             persistStorage  = serviceProvider.GetService<IPersistStorage>();

        var connection = serviceProvider.GetService<SQLiteConnection>();

        await persistStorage.CreateTableAsync();

        var msg = new Message("topic", "hello");

        var removed = await persistStorage.AddToDeadLetterQueueAsync(msg);

        Assert.Equal(1, CountMessageForDeadletterQueue(connection));
        Assert.True(removed);
    }

    [Fact]
    public async Task CreateTable_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbConnectionString; });
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var             persistStorage  = serviceProvider.GetService<IPersistStorage>();

        var connection = serviceProvider.GetService<SQLiteConnection>();

        Assert.Throws<SQLiteException>(() =>
                                       {
                                           using var cmd = new SQLiteCommand(connection);
                                           cmd.CommandText = "select count(*) from memorymq_message;";
                                           cmd.ExecuteScalar();
                                       });

        await persistStorage.CreateTableAsync();
        await using var cmd = new SQLiteCommand(connection);
        cmd.CommandText = "select count(*) from memorymq_message;";
        var count = cmd.ExecuteScalar();
        Assert.Equal(0L, count);
    }

    [Fact]
    public async Task AddMessage_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbConnectionString; });
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var             persistStorage  = serviceProvider.GetService<IPersistStorage>();
        var             connection      = serviceProvider.GetService<SQLiteConnection>();
        await persistStorage.CreateTableAsync();
        var inserted = await persistStorage.AddAsync(new Message("topic", "hello"));

        Assert.Equal(1L,   CountMessage(connection));
        Assert.Equal(true, inserted);
    }

    [Fact]
    public async Task RemoveMessage_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbConnectionString; });
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var             persistStorage  = serviceProvider.GetService<IPersistStorage>();

        var connection = serviceProvider.GetService<SQLiteConnection>();

        await persistStorage.CreateTableAsync();
        var msg = new Message("topic", "hello");
        await persistStorage.AddAsync(msg);

        var removed = await persistStorage.RemoveAsync(msg);

        Assert.Equal(0,    CountMessage(connection));
        Assert.Equal(true, removed);
    }

    [Fact]
    public async Task RestoreMessage_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbConnectionString; });
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var             persistStorage  = serviceProvider.GetService<IPersistStorage>();

        var connection = serviceProvider.GetService<SQLiteConnection>();

        await persistStorage.CreateTableAsync();
        var msg = new Message("topic", "hello");
        await persistStorage.AddAsync(msg);

        Assert.Equal(1, CountMessage(connection));

        var messages = (await persistStorage.RestoreAsync()).ToList();

        Assert.Equal(1, messages.Count);

        Assert.Equal(msg.GetMessageId(), messages[0].GetMessageId());
    }

    [Fact]
    public async Task UpdateRetryMessage_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbConnectionString; });
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var             persistStorage  = serviceProvider.GetService<IPersistStorage>();

        var connection = serviceProvider.GetService<SQLiteConnection>();

        await persistStorage.CreateTableAsync();
        var msg = new Message("topic", "hello");
        await persistStorage.AddAsync(msg);

        msg.IncreaseRetryCount();
        msg.IncreaseRetryCount();
        await persistStorage.UpdateRetryAsync(msg);

        var retryCount = GetMessageRetryCount(connection, msg.GetMessageId());
        Assert.Equal(2, retryCount);
    }

    [Fact]
    public async Task SaveMessage_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbConnectionString; });
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var             persistStorage  = serviceProvider.GetService<IPersistStorage>();

        var connection = serviceProvider.GetService<SQLiteConnection>();

        await persistStorage.CreateTableAsync();

        List<IMessage> msgs =
            new()
            {
                new Message("topic", "hello"),
                new Message("topic", "hello"),
                new Message("topic", "hello"),
            };

        await persistStorage.AddAsync(msgs);

        Assert.Equal(msgs.Count, CountMessage(connection));
    }

    [Fact]
    public async Task Dispose_Passed()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IPersistStorage, SqlitePersistStorage>();

        serviceCollection.AddSingleton<SQLiteConnection>(
                                                         sp =>
                                                             new SQLiteConnection(
                                                                                  sp.GetRequiredService<IOptions<MemoryMQOptions>>().Value
                                                                                    .DbConnectionString
                                                                                 ).OpenAndReturn()
                                                        );

        serviceCollection.Configure<MemoryMQOptions>(it => { it.DbConnectionString = _dbConnectionString; });
        await using var serviceProvider = serviceCollection.BuildServiceProvider();

        var connection = serviceProvider.GetService<SQLiteConnection>();

        await serviceProvider.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => connection.Open());
    }

    private int CountMessage(SQLiteConnection connection)
    {
        using var cmd = new SQLiteCommand(connection);

        cmd.CommandText = "select count(*) from memorymq_message;";
        var count = cmd.ExecuteScalar();

        return int.Parse(count.ToString());
    }

    private int CountMessageForDeadletterQueue(SQLiteConnection connection)
    {
        using var cmd = new SQLiteCommand(connection);

        cmd.CommandText = "select count(*) from memorymq_deadmessage;";
        var count = cmd.ExecuteScalar();

        return int.Parse(count.ToString());
    }

    private int GetMessageRetryCount(SQLiteConnection connection, string id)
    {
        using var cmd = new SQLiteCommand(connection);
        cmd.CommandText = "select retry from memorymq_message where message_id=@id;";

        cmd.Parameters.AddWithValue("@id", id);
        var count = cmd.ExecuteScalar();

        return int.Parse(count.ToString());
    }

    public void Dispose()
    {
        File.Delete(_dbpath);
    }
}