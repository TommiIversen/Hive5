using Microsoft.Extensions.Logging;

namespace Common.DTOs;

public class BaseLogEntry : BaseMessage
{
    public required string Message { get; init; }
    public int LogSequenceNumber { get; set; }
    public LogLevel LogLevel { get; init; } = LogLevel.Information;
    public required DateTime LogTimestamp { get; init; }
}

public class WorkerLogEntry : BaseLogEntry
{
    public required string WorkerId { get; init; }
}

public class EngineLogEntry : BaseLogEntry
{
}
