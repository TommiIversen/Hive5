using Common.DTOs;

namespace Engine.Hubs;

public interface IMessageEnricher
{
    void Enrich(BaseMessage baseMessage, Guid engineId);
}

public class MessageEnricher : IMessageEnricher
{
    private int _sequenceNumber;

    public void Enrich(BaseMessage baseMessage, Guid engineId)
    {
        baseMessage.EngineId = engineId;
        baseMessage.SequenceNumber = GetNextSequenceNumber();
        baseMessage.Timestamp = DateTime.UtcNow; // Sæt Timestamp lige før afsendelse
    }

    private int GetNextSequenceNumber()
    {
        return Interlocked.Increment(ref _sequenceNumber) % int.MaxValue;
    }
}