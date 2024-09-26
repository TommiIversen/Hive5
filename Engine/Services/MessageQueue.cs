using System.Collections.Concurrent;
using Common.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Engine.Services;

public class MessageQueue(int maxQueueSize)
{
    private readonly ConcurrentQueue<BaseMessage> _messageQueue = new();
    private readonly SemaphoreSlim _messageAvailable = new(0);

    public void EnqueueMessage(BaseMessage baseMessage)
    {
        while (_messageQueue.Count >= maxQueueSize)
        {
            _messageQueue.TryDequeue(out _);  // Fjern ældste besked, hvis køen er fyldt
            Console.WriteLine($"MessageQueue: Discarded message due to queue size limit. Queue size: {_messageQueue.Count}");
        }

        _messageQueue.Enqueue(baseMessage);
        _messageAvailable.Release();
    }

    public async Task<BaseMessage?> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        await _messageAvailable.WaitAsync(cancellationToken);
        _messageQueue.TryDequeue(out var message);
        return message;
    }
    
    public int GetQueueSize()
    {
        return _messageQueue.Count;
    }
}

public class MultiQueue
{
    private readonly ILogger<MultiQueue> _logger;
    private readonly ConcurrentDictionary<Type, ConcurrentQueue<BaseMessage>> _queues = new();  
    private readonly ConcurrentDictionary<string, ConcurrentQueue<BaseMessage>> _uniqueQueues = new();
    private readonly int _defaultQueueLimit;
    private readonly SemaphoreSlim _messageAvailable = new(0);  // Semaphore til at signalere nye beskeder
    private const bool DebugFlag = false;

    public MultiQueue(ILogger<MultiQueue> logger, int defaultQueueLimit = 20)
    {
        _logger = logger;
        _defaultQueueLimit = defaultQueueLimit;
    }

    public MultiQueue() : this(new NullLogger<MultiQueue>(), 20)
    {
    }

    public void EnqueueMessage(BaseMessage baseMessage, string uniqueId = "")
    {
        
        if (!string.IsNullOrEmpty(uniqueId))
        {
            var uniqueQueue = _uniqueQueues.GetOrAdd(uniqueId, _ => new ConcurrentQueue<BaseMessage>());  // GetOrAdd sikrer atomisk operation

            lock (uniqueQueue)  // Brug lock her for at sikre trådsikkerhed ved begrænsning af køstørrelse
            {
                if (uniqueQueue.Count >= 1)
                {
                    uniqueQueue.TryDequeue(out _);  // Fjern ældste besked, så vi kun beholder én
                }

                uniqueQueue.Enqueue(baseMessage);  // Tilføj den nye besked

                if (DebugFlag)
                {
                    _logger.LogInformation("Message for UniqueID: {UniqueId} enqueued. Queue size: {QueueSize}", uniqueId, uniqueQueue.Count);
                }
            }
        }
        else
        {
            var messageType = baseMessage?.GetType();

            if (messageType == null)
            {
                Console.WriteLine("Message type is null");
                return;
            }

            var queue = _queues.GetOrAdd(messageType, _ => new ConcurrentQueue<BaseMessage>());  // Brug GetOrAdd for atomisk adgang

            lock (queue)  // Brug lock for at sikre trådsikkerhed ved køstørrelse
            {
                if (baseMessage != null) queue.Enqueue(baseMessage);

                if (queue.Count > _defaultQueueLimit)
                {
                    queue.TryDequeue(out _);  // Fjern ældste besked, hvis køen overskrider grænsen
                }

                if (DebugFlag)
                {
                    _logger.LogInformation("{MessageType} enqueued. Queue size: {QueueSize}", messageType.Name, queue.Count);
                }
            }
        }

        _messageAvailable.Release();
    }

    public async Task<BaseMessage> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _messageAvailable.WaitAsync(cancellationToken);  // Vent på, at der er en besked tilgængelig

            foreach (var uniqueQueue in _uniqueQueues.Values)
            {
                if (uniqueQueue.TryDequeue(out var uniqueMessage))
                {
                    return uniqueMessage;  // Returner unik besked fra uniqueId-kø
                }
            }

            foreach (var queue in _queues.Values)
            {
                if (queue.TryDequeue(out var message))
                {
                    return message;  // Returner besked fra FIFO-kø
                }
            }
        }
    }


    public Dictionary<string, int> ReportQueueContents()
    {
        var report = new Dictionary<string, int>();

        foreach (var queue in _queues)
        {
            report[queue.Key.Name] = queue.Value.Count;
        }

        foreach (var uniqueQueue in _uniqueQueues)
        {
            report[$"Unique-{uniqueQueue.Key}"] = uniqueQueue.Value.Count;
        }

        return report;
    }
}
