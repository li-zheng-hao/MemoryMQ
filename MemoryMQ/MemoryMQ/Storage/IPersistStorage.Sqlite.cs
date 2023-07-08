using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using MemoryMQ.Configuration;
using MemoryMQ.Messages;
using Microsoft.Extensions.Options;

namespace MemoryMQ.Storage;

public class SqlitePersistStorage : IPersistStorage
{
    private SQLiteConnection _connection;

    public SqlitePersistStorage(IOptions<MemoryMQOptions> options)
    {
        _connection = new SQLiteConnection(options.Value.DbConnectionString);
        _connection.Open();
        CreateTable();
    }

    private void CreateTable()
    {
        SQLiteCommand cmd = new SQLiteCommand(_connection);
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
        cmd.ExecuteNonQuery();
    }

    public Task UpdateRetryAsync(IMessage message)
    {
        SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = $@"update memorymq_message set retry={message.GetRetryCount()} where message_id='{message.GetMessageId()}';";
        return cmd.ExecuteNonQueryAsync();
    }

    public Task AddAsync(IMessage message)
    {
        var data = JsonSerializer.Serialize(message);
        SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = $@"insert into memorymq_message (message,message_id,create_time,retry) values ('{data}','{message.GetMessageId()}',{message.GetCreateTime()},{message.GetRetryCount()});";
        return cmd.ExecuteNonQueryAsync();
    }

    public Task RemoveAsync(IMessage message)
    {
        SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = $@"delete from memorymq_message where message_id='{message.GetMessageId()}';";
        return cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<IMessage>> RestoreAsync()
    {
        SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = @"select message from memorymq_message order by create_time asc;";
        var reader = cmd.ExecuteReader();
        var messages = new List<IMessage>();
        while (await reader.ReadAsync())
        {
            var data = reader.GetString(0);
            var message = JsonSerializer.Deserialize<Message>(data);
            if (message != null) messages.Add(message);
        }

        return messages;
    }

    public Task SaveAsync(ICollection<IMessage> message)
    {
       
        StringBuilder sb=new StringBuilder();
        foreach (var m in message)
        {
            var data = JsonSerializer.Serialize(m);
            sb.Append($"insert into memorymq_message (message,message_id,create_time,retry) values ('{data}',{m.GetMessageId()},{m.GetCreateTime()},{m.GetRetryCount()});");
        }
        SQLiteCommand cmd = new SQLiteCommand(_connection);
        cmd.CommandText = sb.ToString();
        return cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}