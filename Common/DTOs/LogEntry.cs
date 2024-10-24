using Microsoft.Extensions.Logging;

namespace Common.DTOs;

public class LogEntry : BaseMessage
{
    public required string WorkerId { get; set; }
    public required string Message { get; set; }
    public int LogSequenceNumber { get; set; }
    
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}