using Common.DTOs;

namespace Engine.Interfaces;

public interface IMessageQueue
{
    void EnqueueMessage(BaseMessage message);
    Task<BaseMessage> DequeueMessageAsync(CancellationToken cancellationToken);
    Task EnqueueMessageAsync(BaseMessage message, CancellationToken cancellationToken);
}