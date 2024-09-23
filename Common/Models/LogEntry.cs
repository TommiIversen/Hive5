// Models/LogEntry.cs
namespace Common.Models;

public class LogEntry: BaseMessage
{
    public required Guid WorkerId { get; set; }
    public required string Message { get; set; }
}