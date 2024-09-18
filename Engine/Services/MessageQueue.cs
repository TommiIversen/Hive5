using System.Collections.Concurrent;
using Common.Models;

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
        // Tjek om køen har overskredet den maksimale størrelse
        while (_messageQueue.Count >= _maxQueueSize)
        {
            // Fjern den ældste besked (smid væk)
            _messageQueue.TryDequeue(out var discardedMessage);
            Console.WriteLine($"MessageQueue: Discarded message due to queue size limit. Queue size: {_messageQueue.Count}");
        }

        // Tilføj ny besked til køen
        _messageQueue.Enqueue(message);
        Console.WriteLine($"MessageQueue: Message enqueued. Queue size: {_messageQueue.Count}");

        // Signal at der er en ny besked tilgængelig
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
    // Hver kø består af FIFO-køer for almindelige beskeder og køer pr. unikt ID
    private readonly ConcurrentDictionary<Type, Queue<IMessage>> _queues = new();
    
    // Køer til unikke beskeder baseret på uniqueId (kun én besked pr. unikt ID)
    private readonly ConcurrentDictionary<string, Queue<IMessage>> _uniqueQueues = new();

    // Begrænsning på antal elementer pr. kø
    private readonly int _defaultQueueLimit;
    private readonly SemaphoreSlim _messageAvailable = new(0);  // Semaphore til at signalere nye beskeder

    public MultiQueue(int defaultQueueLimit = 20)
    {
        _defaultQueueLimit = defaultQueueLimit;
    }

    // Tilføj en besked med en unik identifikator for unikke beskeder (f.eks. WorkerId)
    public void EnqueueMessage(IMessage message, string uniqueId = "")
    {
        if (!string.IsNullOrEmpty(uniqueId))
        {
            // Håndtering af unikke elementer med en kø af størrelse 1
            if (!_uniqueQueues.ContainsKey(uniqueId))
            {
                _uniqueQueues[uniqueId] = new Queue<IMessage>(1);  // Opret en ny kø for uniqueId
            }

            var uniqueQueue = _uniqueQueues[uniqueId];

            // Hvis køen allerede har en besked, fjern den (så vi kun har én besked pr. uniqueId)
            if (uniqueQueue.Count > 0)
            {
                uniqueQueue.Dequeue();  // Fjern ældste besked, så vi kun beholder én
            }

            uniqueQueue.Enqueue(message);  // Tilføj den nye besked

            Console.WriteLine($"Message for UniqueID: {uniqueId} enqueued. Queue size: {uniqueQueue.Count}");
        }
        else
        {
            var messageType = message.GetType();

            // Håndtering af almindelig kø (FIFO)
            if (!_queues.ContainsKey(messageType))
            {
                _queues[messageType] = new Queue<IMessage>();
            }

            var queue = _queues[messageType];

            // Tilføj beskeden til køen
            queue.Enqueue(message);

            // Begræns køen til max antal elementer
            if (queue.Count > _defaultQueueLimit)
            {
                queue.Dequeue();  // Fjern ældste besked, hvis køen overskrider grænsen
            }

            Console.WriteLine($"{messageType.Name} enqueued. Queue size: {queue.Count}");
        }

        // Signalér at der er en ny besked tilgængelig
        _messageAvailable.Release();
    }

    // Dequeue fra hvilken som helst kø uden delay
    public async Task<IMessage> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        // Vent indtil der er en ny besked tilgængelig
        await _messageAvailable.WaitAsync(cancellationToken);

        // Først tjek unikke beskeder baseret på uniqueId
        foreach (var uniqueQueue in _uniqueQueues.Values)
        {
            if (uniqueQueue.Count > 0)
            {
                return uniqueQueue.Dequeue();  // Returner unik besked fra uniqueId-kø
            }
        }

        // Gå gennem FIFO-køer bagefter
        foreach (var queue in _queues.Values)
        {
            if (queue.Count > 0)
            {
                return queue.Dequeue();  // Returner besked fra FIFO-kø
            }
        }

        return null;  // Hvis ingen beskeder er tilgængelige
    }

    // Hent antallet af elementer i køen pr. beskedtype og pr. uniqueId
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

