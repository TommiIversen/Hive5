using System.Collections.Concurrent;
using Common.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Engine.Services;

public class MessageQueue
{
    private readonly ConcurrentQueue<IMessage> _messageQueue = new();
    private readonly SemaphoreSlim _messageAvailable = new(0);
    private readonly int _maxQueueSize;

    public MessageQueue(int maxQueueSize)
    {
        _maxQueueSize = maxQueueSize;
    }

    public void EnqueueMessage(IMessage message)
    {
        while (_messageQueue.Count >= _maxQueueSize)
        {
            _messageQueue.TryDequeue(out var discardedMessage);
            Console.WriteLine($"MessageQueue: Discarded message due to queue size limit. Queue size: {_messageQueue.Count}");
        }

        _messageQueue.Enqueue(message);
        Console.WriteLine($"MessageQueue: Message enqueued. Queue size: {_messageQueue.Count}");
        _messageAvailable.Release();
    }

    public async Task<IMessage> DequeueMessageAsync(CancellationToken cancellationToken)
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
    private readonly ConcurrentDictionary<Type, Queue<IMessage>> _queues = new();
    private readonly ConcurrentDictionary<string, Queue<IMessage>> _uniqueQueues = new();
    private readonly int _defaultQueueLimit;
    private readonly SemaphoreSlim _messageAvailable = new(0);  // Semaphore til at signalere nye beskeder

    public MultiQueue(ILogger<MultiQueue> logger, int defaultQueueLimit = 20)
    {
        _logger = logger;
        _defaultQueueLimit = defaultQueueLimit;
    }

    public MultiQueue() : this(new NullLogger<MultiQueue>(), 20)
    {
    }

    public void EnqueueMessage(IMessage message, string uniqueId = "")
    {
        if (!string.IsNullOrEmpty(uniqueId))
        {
            if (!_uniqueQueues.ContainsKey(uniqueId))
            {
                _uniqueQueues[uniqueId] = new Queue<IMessage>(1);  // Opret en ny kø for uniqueId
            }

            var uniqueQueue = _uniqueQueues[uniqueId];
            if (uniqueQueue.Count > 0)
            {
                uniqueQueue.Dequeue();  // Fjern ældste besked, så vi kun beholder én
            }

            uniqueQueue.Enqueue(message);  // Tilføj den nye besked
            _logger.LogInformation("Message for UniqueID: {UniqueId} enqueued. Queue size: {QueueSize}", uniqueId, uniqueQueue.Count);
        }
        else
        {
            var messageType = message.GetType();
            if (!_queues.ContainsKey(messageType))
            {
                _queues[messageType] = new Queue<IMessage>();
            }

            var queue = _queues[messageType];
            queue.Enqueue(message);

            if (queue.Count > _defaultQueueLimit)
            {
                queue.Dequeue();  // Fjern ældste besked, hvis køen overskrider grænsen
            }
            _logger.LogInformation("{MessageType} enqueued. Queue size: {QueueSize}", messageType.Name, queue.Count);
        }
        _messageAvailable.Release();
    }

    public async Task<IMessage> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        await _messageAvailable.WaitAsync(cancellationToken);
        foreach (var uniqueQueue in _uniqueQueues.Values)
        {
            if (uniqueQueue.Count > 0)
            {
                return uniqueQueue.Dequeue();  // Returner unik besked fra uniqueId-kø
            }
        }

        foreach (var queue in _queues.Values)
        {
            if (queue.Count > 0)
            {
                return queue.Dequeue();  // Returner besked fra FIFO-kø
            }
        }

        return null;  // Hvis ingen beskeder er tilgængelige
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

