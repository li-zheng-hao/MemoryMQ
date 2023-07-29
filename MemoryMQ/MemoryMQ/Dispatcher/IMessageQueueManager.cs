using System.Threading.Channels;
using MemoryMQ.Configuration;
using MemoryMQ.Messages;

namespace MemoryMQ.Dispatcher;

public interface IMessageQueueManager
{
    /// <summary>
    ///  init consumer queues and start listening received messages
    /// </summary>
    /// <returns>all topics</returns>
    List<string> InitConsumers();

    /// <summary>
    /// enqueue message according to topic
    /// </summary>
    /// <param name="message"></param>
    /// <param name="useCompress"></param>
    /// <param name="insertStorage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<bool> EnqueueAsync(
        IMessage          message,
        bool              useCompress       = true,
        bool              insertStorage     = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// enqueue messages according to topic
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="useCompress"></param>
    /// <param name="insertStorage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<bool> EnqueueAsync(
        IEnumerable<IMessage> messages,
        bool                  useCompress       = true,
        bool                  insertStorage     = true,
        CancellationToken     cancellationToken = default
    );

    /// <summary>
    /// Message Received Event
    /// </summary>
    Func<IMessage, MessageOptions, CancellationToken, Task>? MessageReceivedEvent { get; set; }

    /// <summary>
    ///  start receiving messages
    /// </summary>
    /// <param name="cancellationToken"></param>
    void Listen(CancellationToken cancellationToken);
}