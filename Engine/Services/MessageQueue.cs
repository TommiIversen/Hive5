using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Engine.Services
{
    public class MessageQueue
    {
        private readonly ConcurrentQueue<object> _messageQueue = new();
        private readonly SemaphoreSlim _messageAvailable = new(0);

        public void EnqueueMessage(object message)
        {
            _messageQueue.Enqueue(message);
            _messageAvailable.Release();  // Signal that a message is available
        }

        public async Task<object> DequeueMessageAsync(CancellationToken cancellationToken)
        {
            await _messageAvailable.WaitAsync(cancellationToken);
            _messageQueue.TryDequeue(out var message);
            return message;
        }
    }
}