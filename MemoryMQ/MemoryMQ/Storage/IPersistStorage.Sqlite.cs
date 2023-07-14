using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using MemoryMQ.Configuration;
using MemoryMQ.Messages;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Storage;

public class SqlitePersistStorage : IPersistStorage
{
    private readonly SQLiteConnection _connection;

    public SqlitePersistStorage(IOptions<MemoryMQOptions> options, SQLiteConnection connection)
    {
        _connection = connection;
    }

    public async Task CreateTableAsync()
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);

        cmd.CommandText = @"
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

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UpdateRetryAsync(IMessage message)
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = $@"update memorymq_message set retry={message.GetRetryCount()} where message_id='{message.GetMessageId()}';";

        return (await cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<bool> AddAsync(IMessage message)
    {
        var data = JsonSerializer.Serialize(message);
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = $@"insert into memorymq_message (message,message_id,create_time,retry) values ('{data}','{message.GetMessageId()}',{message.GetCreateTime()},{message.GetRetryCount()});";

        return (await cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<bool> RemoveAsync(IMessage message)
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = $@"delete from memorymq_message where message_id='{message.GetMessageId()}';";

        return (await cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<IEnumerable<IMessage>> RestoreAsync()
    {
        await using SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = @"select message from memorymq_message order by create_time asc;";
        await using var reader = cmd.ExecuteReader();
        var messages = new List<IMessage>();

        while (await reader.ReadAsync())
        {
            var data = reader.GetString(0);
            var message = JsonSerializer.Deserialize<Message>(data);
            if (message != null) messages.Add(message);
        }

        return messages;
    }

    public async Task<bool> AddAsync(ICollection<IMessage> message)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var m in message)
        {
            var data = JsonSerializer.Serialize(m);
            sb.Append($"insert into memorymq_message (message,message_id,create_time,retry) values ('{data}','{m.GetMessageId()}',{m.GetCreateTime()},{m.GetRetryCount()});");
        }

        var sqlComm = new SQLiteCommand("begin", _connection);
        sqlComm.ExecuteNonQuery();

        await using SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = sb.ToString();
        var insertedNum = await cmd.ExecuteNonQueryAsync();
        sqlComm = new SQLiteCommand("end", _connection);
        sqlComm.ExecuteNonQuery();

        return insertedNum == message.Count;
    }
}