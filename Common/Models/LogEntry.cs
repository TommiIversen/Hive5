// Models/LogEntry.cs
namespace Common.Models;

public class LogEntry: IMessage
{
    public Guid EngineId { get; set; }
    public required Guid WorkerId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required string Message { get; set; }
}