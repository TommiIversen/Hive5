namespace Common.Models;

public class BaseMessage
{
    public Guid EngineId { get; set; }
    public required DateTime Timestamp { get; set; }
    
}