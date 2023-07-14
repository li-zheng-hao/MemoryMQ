using MemoryMQ.Configuration;
using MemoryMQ.Internal;
using MemoryMQ.Messages;
using MemoryMQ.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryMQ.RetryStrategy;

public class DefaultRetryStrategy : IRetryStrategy
{
    private readonly ILogger<DefaultRetryStrategy> _logger;


    private readonly IOptions<MemoryMQOptions> _options;

    private readonly IPersistStorage? _persistStorage;

    public Func<IMessage, Task> MessageRetryEvent { get; set; }
    
    public Func<IMessage, Task> MessageRetryFailureEvent { get; set; }

    /// <summary>
    /// Retry queue, If restarted, the queue will be cleared and all messages will be consumed again after restored from database
    /// </summary>
    private readonly PriorityQueue<IMessage, long> _retryChannel;

    private readonly object schedulerLock = new();

    private bool isSchedulerRunning;

    public DefaultRetryStrategy(ILogger<DefaultRetryStrategy> logger, IOptions<MemoryMQOptions> options,
        IPersistStorage persistStorage = null)
    {
        _logger = logger;
        _options = options;
        _persistStorage = persistStorage;
        _retryChannel = new PriorityQueue<IMessage, long>();
    }

    private void RunRetryScheduler(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (_retryChannel.TryPeek(out _, out var ticks))
                {
                    await DequeueAndConsume(ticks);
                }

                await Task.Delay(500, cancellationToken);
            }
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private async ValueTask DequeueAndConsume(long ticks)
    {
        try
        {
            if (ticks >= DateTime.Now.Ticks) return;

            _retryChannel.TryDequeue(out var msg, out _);

            if (msg is null) return;

            _logger.LogInformation("retry message id: {MessageId} topic: {Topic} retry count {RetryCount}", msg.GetMessageId(), msg.GetTopic(), msg.GetRetryCount());

            if (MessageRetryEvent != null) await MessageRetryEvent(msg);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "retry message error");
        }
    }

    public async Task ScheduleRetryAsync(IMessage message, MessageOptions messageOptions,CancellationToken cancellationToken)
    {
        if (!isSchedulerRunning)
        {
            lock (schedulerLock)
            {
                if (!isSchedulerRunning)
                {
                    RunRetryScheduler(cancellationToken);

                    isSchedulerRunning = true;
                }
            }
        }
        
        var retryCount = messageOptions.GetRetryCount(_options.Value);

        // dont need retry
        if (retryCount == 0)
        {
            if (messageOptions.GetEnablePersistence(_options.Value)) 
                await _persistStorage!.RemoveAsync(message);

            return;
        }

        // retry failure message
        if (message.GetRetryCount() > retryCount)
        {
            if (MessageRetryFailureEvent != null) await MessageRetryFailureEvent(message);
        }
        // retry again
        else
        {
            EnqueueRetryQueue(message);

            if (messageOptions.GetEnablePersistence(_options.Value)) await _persistStorage!.UpdateRetryAsync(message);
        }
    }

    private void EnqueueRetryQueue(IMessage message)
    {
        var retryCount = message.GetRetryCount()!;

        TimeSpan delay;

        switch (_options.Value.RetryMode)
        {
            case RetryMode.Fixed:
                delay = _options.Value.RetryInterval;

                break;
            case RetryMode.Exponential:
                delay = _options.Value.RetryInterval * Math.Pow(2, retryCount.Value);

                break;
            case RetryMode.Incremental:
                delay = _options.Value.RetryInterval * retryCount.Value;

                break;
            default:
                throw new ArgumentOutOfRangeException(_options.Value.RetryMode.ToString());
        }

        _logger.LogInformation("message {MessageId} next retry interval is {DelayTotalSeconds} seconds, it will trigger at {Add}",
            message.GetMessageId(), delay.TotalSeconds, DateTime.Now.Add(delay));

        _retryChannel.Enqueue(message, DateTime.Now.Add(delay).Ticks);
    }
}