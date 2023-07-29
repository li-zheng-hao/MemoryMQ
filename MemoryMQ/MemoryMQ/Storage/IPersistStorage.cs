using MemoryMQ.Messages;

namespace MemoryMQ.Storage;

/// <summary>
/// 
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
    /// remove message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> RemoveAsync(IMessage message);

    /// <summary>
    /// restore message from database
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<IMessage>> RestoreAsync();

    /// <summary>
    /// add new message
    /// </summary>
    /// <param name="message"></param>
    Task<bool> AddAsync(ICollection<IMessage> message);

    /// <summary>
    /// add new message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<bool> AddAsync(IMessage message);

    /// <summary>
    /// Adds the specified message to the dead letter queue.
    /// </summary>
    Task<bool> AddToDeadLetterQueueAsync(IMessage message);
}