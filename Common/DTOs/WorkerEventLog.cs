using MessagePack;

namespace Common.DTOs;

[MessagePackObject]
public class EventLogEntry
{
    [Key(0)] public required string Message { get; set; }

    [Key(1)] public DateTime LogTimestamp { get; set; }

    [Key(2)] public int LogLevel { get; set; } = 1;
}

[MessagePackObject]
public class WorkerEventLogDto
{
    [Key(0)] public DateTime EventTimestamp { get; set; }

    [Key(1)] public required string EventMessage { get; set; } = string.Empty;

    [Key(2)] public List<EventLogEntry> Logs { get; set; } = [];
}

[MessagePackObject]
public class WorkerEventWithLogsDto
{
    [Key(0)] public required string WorkerId { get; set; } = string.Empty;

    [Key(1)] public List<WorkerEventLogDto> Events { get; set; } = [];
}