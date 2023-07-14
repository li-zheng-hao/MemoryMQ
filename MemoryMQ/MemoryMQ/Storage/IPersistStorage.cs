using MemoryMQ.Messages;

namespace MemoryMQ.Storage;

public interface IPersistStorage
{
    /// <summary>
    /// create table if not exists
    /// </summary>
    /// <returns></returns>
    Task CreateTableAsync();
    
    /// <summary>
    /// update retry count
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> UpdateRetryAsync(IMessage message);
    
    Task<bool> AddAsync(IMessage message);
    
    Task<bool> RemoveAsync(IMessage message);
    
    /// <summary>
    /// 恢复
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<IMessage>> RestoreAsync();
    
    /// <summary>
    /// 存储
    /// </summary>
    /// <param name="message"></param>
    Task<bool> AddAsync(ICollection<IMessage> message);
}
