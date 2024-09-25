namespace Common.Models;

public class BaseMessage
{
    public Guid EngineId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}