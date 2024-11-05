using Microsoft.Extensions.Logging;

namespace Common.DTOs;

public class BaseLogEntry : BaseMessage
{
    public required string Message { get; set; }
    public int LogSequenceNumber { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public DateTime LogTimestamp { get; set; }
}

public class WorkerLogEntry : BaseLogEntry
{
    public required string WorkerId { get; set; }
}

public class EngineLogEntry : BaseLogEntry
{
}
