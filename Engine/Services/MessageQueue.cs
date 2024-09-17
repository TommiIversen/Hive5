using System.Collections.Concurrent;
using Common.Models;

namespace Engine.Services;

public class MessageQueue
{
    private readonly ConcurrentQueue<IMessage> _messageQueue = new();
    private readonly SemaphoreSlim _messageAvailable = new(0);

    public void EnqueueMessage(IMessage message)
    {
        _messageQueue.Enqueue(message);
        _messageAvailable.Release();  // Signal that a message is available
    }

    public async Task<IMessage> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        await _messageAvailable.WaitAsync(cancellationToken);
        _messageQueue.TryDequeue(out var message);
        return message;
    }
}