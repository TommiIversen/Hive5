using MessagePack;
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

[MessagePackObject]
public class EventLogEntry
{
    [Key(0)] public required string Message { get; set; }

    [Key(1)] public DateTime LogTimestamp { get; set; }

    [Key(2)] public int LogLevel { get; set; } = 1; // Brug int for log level
}

[MessagePackObject]
public class WorkerEventLogDto
{
    [Key(0)] public DateTime EventTimestamp { get; set; }

    [Key(1)] public required string EventMessage { get; set; } = string.Empty;

    [Key(2)] public List<EventLogEntry> Logs { get; set; } = new(); // Hver event har en liste af logs
}

[MessagePackObject]
public class WorkerEventWithLogsDto
{
    [Key(0)] public required string WorkerId { get; set; } = string.Empty;

    [Key(1)] public List<WorkerEventLogDto> Events { get; set; } = new(); // De seneste 20 events for worker
}