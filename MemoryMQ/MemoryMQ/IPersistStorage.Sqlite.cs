namespace MemoryMQ;

public class SqlitePersistStorage:IPersistStorage
{
    public Task AddAsync(IMessage message)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(IMessage message)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IMessage>> RestoreAsync()
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(ICollection<IMessage> message)
    {
        throw new NotImplementedException();
    }
}