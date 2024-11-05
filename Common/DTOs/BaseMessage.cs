using MessagePack;

namespace Common.DTOs;

public class BaseMessage
{
    [Key(0)] public Guid EngineId { get; set; }
    [Key(1)]public DateTime Timestamp { get; set; }
    [Key(2)]public int SequenceNumber { get; set; }
}