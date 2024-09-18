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