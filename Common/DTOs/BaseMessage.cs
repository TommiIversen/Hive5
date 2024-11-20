namespace Common.DTOs;

public class BaseMessage
{
    public Guid EngineId { get; set; }
    public DateTime Timestamp { get; set; }
    public int SequenceNumber { get; set; }
}