using MemoryMQ.Messages;

namespace MemoryMQ.Storage;

/// <summary>
/// persist message to database
/// </summary>
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
    
    /// <summary>
    /// insert a message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> AddAsync(IMessage message);
    
    /// <summary>
    /// remove a message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> RemoveAsync(IMessage message);
    
    /// <summary>
    /// restore messages from database
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<IMessage>> RestoreAsync();
    
    /// <summary>
    /// insert batch messages
    /// </summary>
    /// <param name="message"></param>
    Task<bool> AddAsync(ICollection<IMessage> message);
}
