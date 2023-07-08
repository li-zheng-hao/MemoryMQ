namespace MemoryMQ;

public interface IPersistStorage:IDisposable
{
    /// <summary>
    /// update retry count
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task UpdateRetryAsync(IMessage message);
    
    Task AddAsync(IMessage message);
    
    Task RemoveAsync(IMessage message);
    
    /// <summary>
    /// 恢复
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<IMessage>> RestoreAsync();
    
    /// <summary>
    /// 存储
    /// </summary>
    /// <param name="message"></param>
    Task SaveAsync(ICollection<IMessage> message);
}
