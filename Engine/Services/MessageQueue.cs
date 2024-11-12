using System.Collections.Concurrent;
using System.Threading.Channels;
using Common.DTOs;
using Engine.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Engine.Services;

public class MessageQueue : IMessageQueue
{
    private readonly Channel<BaseMessage> _messageChannel;

    public MessageQueue(int maxQueueSize)
    {
        // BoundedChannel med købegrænsning og ældste besked bliver fjernet, hvis køen er fuld
        var options = new BoundedChannelOptions(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest // Fjern ældste besked, når køen er fuld
        };
        _messageChannel = Channel.CreateBounded<BaseMessage>(options);
    }

    public void EnqueueMessage(BaseMessage baseMessage)
    {
        // Vi bruger TryWrite for synkron håndtering. Returnerer false, hvis køen er fuld
        if (!_messageChannel.Writer.TryWrite(baseMessage))
            Console.WriteLine("MessageQueue: Queue is full, dropping oldest message.");
    }

    public async Task EnqueueMessageAsync(BaseMessage baseMessage, CancellationToken cancellationToken)
    {
        // Vent asynkront på, at der er plads i kanalen, hvis den er fuld
        await _messageChannel.Writer.WriteAsync(baseMessage, cancellationToken);
    }

    public async Task<BaseMessage> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        // Vent på at modtage en besked fra køen
        return await _messageChannel.Reader.ReadAsync(cancellationToken);
    }

    public int GetQueueSize()
    {
        // Returnér antallet af beskeder i køen
        return _messageChannel.Reader.Count;
    }

    public void EnqueueMessage(BaseLogEntry baseLogEntry)
    {
        throw new NotImplementedException();
    }
}

public class MultiQueue
{
    private readonly int _defaultQueueLimit;
    private readonly ILogger<MultiQueue> _logger;
    private readonly SemaphoreSlim _messageAvailable = new(0); // Semaphore til at signalere nye beskeder
    private readonly ConcurrentDictionary<Type, ConcurrentQueue<BaseMessage>> _queues = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<BaseMessage>> _uniqueQueues = new();

    public MultiQueue(ILogger<MultiQueue> logger, int defaultQueueLimit = 20)
    {
        _logger = logger;
        _defaultQueueLimit = defaultQueueLimit;
    }

    public MultiQueue() : this(new NullLogger<MultiQueue>())
    {
    }

    public void EnqueueMessage(BaseMessage baseMessage, string uniqueId = "")
    {
        if (!string.IsNullOrEmpty(uniqueId))
        {
            var uniqueQueue =
                _uniqueQueues.GetOrAdd(uniqueId,
                    _ => new ConcurrentQueue<BaseMessage>()); // GetOrAdd sikrer atomisk operation

            lock (uniqueQueue) // Brug lock her for at sikre trådsikkerhed ved begrænsning af køstørrelse
            {
                if (uniqueQueue.Count >= 1) uniqueQueue.TryDequeue(out _); // Fjern ældste besked, så vi kun beholder én

                uniqueQueue.Enqueue(baseMessage); // Tilføj den nye besked
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

            var queue = _queues.GetOrAdd(messageType,
                _ => new ConcurrentQueue<BaseMessage>()); // Brug GetOrAdd for atomisk adgang

            lock (queue) // Brug lock for at sikre trådsikkerhed ved køstørrelse
            {
                if (baseMessage != null) queue.Enqueue(baseMessage);

                if (queue.Count > _defaultQueueLimit)
                    queue.TryDequeue(out _); // Fjern ældste besked, hvis køen overskrider grænsen
            }
        }

        _messageAvailable.Release();
    }

    public async Task<BaseMessage> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _messageAvailable.WaitAsync(cancellationToken); // Vent på, at der er en besked tilgængelig

            foreach (var uniqueQueue in _uniqueQueues.Values)
                if (uniqueQueue.TryDequeue(out var uniqueMessage))
                    return uniqueMessage; // Returner unik besked fra uniqueId-kø

            foreach (var queue in _queues.Values)
                if (queue.TryDequeue(out var message))
                    return message; // Returner besked fra FIFO-kø
        }
    }


    public int GetQueueSizeForType(Type messageType)
    {
        return _queues.TryGetValue(messageType, out var queue) ? queue.Count : 0;
    }

    public int GetUniqueQueueSize(string uniqueId)
    {
        return _uniqueQueues.TryGetValue(uniqueId, out var queue) ? queue.Count : 0;
    }
}