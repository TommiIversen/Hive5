using MessagePack;

namespace Common.DTOs;

[MessagePackObject]
public class EventLogEntry
{
    [Key(0)] public required string Message { get; init; }

    [Key(1)] public DateTime LogTimestamp { get; init; }

    [Key(2)] public int LogLevel { get; init; } = 1;
}

[MessagePackObject]
public class WorkerEventLogDto
{
    [Key(0)] public DateTime EventTimestamp { get; init; }

    [Key(1)] public required string EventMessage { get; init; } = string.Empty;

    [Key(2)] public List<EventLogEntry> Logs { get; init; } = [];
}

[MessagePackObject]
public class WorkerEventWithLogsDto
{
    [Key(1)] public List<WorkerEventLogDto> Events { get; init; } = [];
}