using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using MemoryMQ.Configuration;
using MemoryMQ.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Storage;

public class SqlitePersistStorage : IPersistStorage
{
    private readonly ILogger<SqlitePersistStorage> _logger;

    private readonly IOptions<MemoryMQOptions> _options;

    private readonly SQLiteConnection _connection;

    public SqlitePersistStorage(
        ILogger<SqlitePersistStorage> logger,
        IOptions<MemoryMQOptions>     options,
        SQLiteConnection              connection
    )
    {
        _logger     = logger;
        _options    = options;
        _connection = connection;
        SpeedupSqlite();
    }

    private void SpeedupSqlite()
    {
        using var cmd = new SQLiteCommand(_connection);
        cmd.CommandText = "PRAGMA journal_mode = WAL;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA synchronous = NORMAL;";
        cmd.ExecuteNonQuery();
    }

    public async Task CreateTableAsync()
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);

        cmd.CommandText =
            @"
create table if not exists memorymq_message
(
    id          INTEGER
        constraint table_name_pk
            primary key autoincrement,
    message     TEXT    not null,
    message_id  TEXT    not null,
    create_time INTEGER not null,
    retry       INTEGER not null 
);

create unique index if not exists memorymq_message_message_id_index 
    on memorymq_message (message_id);
        ";

        // add dead letter queue table
        if (_options.Value.EnableDeadLetterQueue)
        {
            cmd.CommandText +=
                @"

create table if not exists memorymq_deadmessage
(
    id          INTEGER
        constraint table_name_pk
            primary key autoincrement,
    message     TEXT    not null,
    message_id  TEXT    not null,
    create_time INTEGER not null,
    retry       INTEGER not null 
);

";
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UpdateRetryAsync(IMessage message)
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);

        cmd.CommandText =
            $@"update memorymq_message set retry={message.GetRetryCount()} where message_id='{message.GetMessageId()}';";

        return (await cmd.ExecuteNonQueryAsync()) > 0;
    }


    public async Task<bool> RemoveAsync(IMessage message)
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);

        cmd.CommandText =
            $@"delete from memorymq_message where message_id='{message.GetMessageId()}';";

        return (await cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<IEnumerable<IMessage>> RestoreAsync()
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = @"select message,retry from memorymq_message order by create_time asc;";
        await using var reader   = cmd.ExecuteReader();
        var             messages = new List<IMessage>();

        while (await reader.ReadAsync())
        {
            var data       = reader.GetString(0);
            var retryCount = reader.GetInt64(1);

            var message = JsonSerializer.Deserialize<Message>(data);

            if (message == null)
                continue;

            message.SetRetryCount(retryCount);
            messages.Add(message);
        }

        return messages;
    }

    public async Task<bool> AddAsync(ICollection<IMessage> message)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("insert into memorymq_message (message,message_id,create_time,retry) values ");

        foreach (var m in message)
        {
            var data = JsonSerializer.Serialize(m);

            sb.Append($" ('{data}','{m.GetMessageId()}',{m.GetCreateTime()},{m.GetRetryCount()}),");
        }

        sb.Remove(sb.Length - 1, 1);

        await using var transaction = _connection.BeginTransaction();

        try
        {
            await using var sqlComm = new SQLiteCommand(_connection);

            sqlComm.CommandText = sb.ToString();

            await sqlComm.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "batch insert message error");

            await transaction.RollbackAsync();

            return false;
        }
    }

    public async Task<bool> AddAsync(IMessage message)
    {
        var                       data = JsonSerializer.Serialize(message);
        await using SQLiteCommand cmd  = new SQLiteCommand(_connection);

        cmd.CommandText =
            $@"insert into memorymq_message (message,message_id,create_time,retry) values ('{data}','{message.GetMessageId()}',{message.GetCreateTime()},{message.GetRetryCount()});";

        return (await cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<bool> AddToDeadLetterQueueAsync(IMessage message)
    {
        var                       data = JsonSerializer.Serialize(message);
        await using SQLiteCommand cmd  = new SQLiteCommand(_connection);

        cmd.CommandText =
            $@"insert into memorymq_deadmessage (message,message_id,create_time,retry) values ('{data}','{message.GetMessageId()}',{message.GetCreateTime()},{message.GetRetryCount()});";

        return (await cmd.ExecuteNonQueryAsync()) > 0;
    }
}