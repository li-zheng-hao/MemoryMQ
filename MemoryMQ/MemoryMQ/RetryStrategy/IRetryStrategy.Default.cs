using MemoryMQ.Configuration;
using MemoryMQ.Dispatcher;
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

    private readonly IMessageQueueManager _queueManager;

    private readonly IPersistStorage? _persistStorage;

    public Func<IMessage, Task>? MessageRetryFailureEvent { get; set; }

    /// <summary>
    /// Retry queue, If restarted, the queue will be cleared and all messages will be consumed again after restored from database
    /// </summary>
    private readonly PriorityQueue<IMessage, long> _retryChannel;

    private readonly object _schedulerLock = new();

    private bool _isSchedulerRunning;

    public DefaultRetryStrategy(
        ILogger<DefaultRetryStrategy> logger,
        IOptions<MemoryMQOptions>     options,
        IMessageQueueManager          queueManager,
        IPersistStorage?              persistStorage = null
    )
    {
        _logger         = logger;
        _options        = options;
        _queueManager   = queueManager;
        _persistStorage = persistStorage;
        _retryChannel   = new PriorityQueue<IMessage, long>();
    }

    private void RunRetryScheduler(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(
                              async () =>
                              {
                                  while (!cancellationToken.IsCancellationRequested)
                                  {
                                      while (_retryChannel.TryPeek(out _, out var ticks))
                                      {
                                          await DequeueAndConsume(ticks);
                                      }

                                      await Task.Delay(500, cancellationToken);
                                  }
                              },
                              cancellationToken,
                              TaskCreationOptions.LongRunning,
                              TaskScheduler.Default
                             );
    }

    private async ValueTask DequeueAndConsume(long ticks)
    {
        try
        {
            if (ticks >= DateTime.Now.Ticks)
                return;

            _retryChannel.TryDequeue(out var msg, out _);

            if (msg is null)
                return;

            _logger.LogInformation(
                                   "retry message id: {MessageId} topic: {Topic} retry count {RetryCount}",
                                   msg.GetMessageId(),
                                   msg.GetTopic(),
                                   msg.GetRetryCount()
                                  );

            await _queueManager.EnqueueAsync(msg, false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "retry message error");
        }
    }

    public async Task<bool> ScheduleRetryAsync(
        IMessage          message,
        MessageOptions    messageOptions,
        CancellationToken cancellationToken
    )
    {
        StartRetryScheduler(cancellationToken);

        var retryCount = messageOptions.GetRetryCount(_options.Value);

        // dont need retry
        if (retryCount == 0)
        {
            if (messageOptions.GetEnablePersistence(_options.Value))
                return await _persistStorage!.RemoveAsync(message);

            return true;
        }

        // retry failure message
        if (message.GetRetryCount() > retryCount)
        {
            return await HandleMaxRetriesMessage(message, messageOptions);
        }

        // retry again
        else
        {
            EnqueueRetryQueue(message);

            if (messageOptions.GetEnablePersistence(_options.Value))
                return await _persistStorage!.UpdateRetryAsync(message);

            return true;
        }
    }

    private async Task<bool> HandleMaxRetriesMessage(
        IMessage       message,
        MessageOptions messageOptions
    )
    {
        if (MessageRetryFailureEvent != null)
            await MessageRetryFailureEvent(message);

        bool operationResult = true;

        if (messageOptions.GetEnablePersistence(_options.Value))
            operationResult &= await _persistStorage!.RemoveAsync(message);

        if (
            messageOptions.GetEnablePersistence(_options.Value)
            && _options.Value.EnableDeadLetterQueue
        )
            operationResult &= await _persistStorage!.AddToDeadLetterQueueAsync(message);

        return operationResult;
    }

    private void StartRetryScheduler(CancellationToken cancellationToken)
    {
        if (_isSchedulerRunning)
            return;

        lock (_schedulerLock)
        {
            if (_isSchedulerRunning)
                return;

            RunRetryScheduler(cancellationToken);

            _isSchedulerRunning = true;
        }
    }

    private void EnqueueRetryQueue(IMessage message)
    {
        var retryCount = message.GetRetryCount()!;

        TimeSpan delay = _options.Value.RetryMode switch
                         {
                             RetryMode.Fixed       => _options.Value.RetryInterval,
                             RetryMode.Exponential => _options.Value.RetryInterval * Math.Pow(2, retryCount.Value),
                             RetryMode.Incremental => _options.Value.RetryInterval * retryCount.Value,
                             _                     => throw new ArgumentOutOfRangeException(_options.Value.RetryMode.ToString())
                         };

        _logger.LogInformation(
                               "message {MessageId} next retry interval is {DelayTotalSeconds} seconds, it will trigger at {Add}",
                               message.GetMessageId(),
                               delay.TotalSeconds,
                               DateTime.Now.Add(delay)
                              );

        _retryChannel.Enqueue(message, DateTime.Now.Add(delay).Ticks);
    }
}