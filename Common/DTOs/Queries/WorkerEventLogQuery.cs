using MessagePack;

namespace Common.DTOs.Queries;

[MessagePackObject]
public class EventLogEntry
{
    public required string Message { get; init; }

    public DateTime LogTimestamp { get; init; }

    public int LogLevel { get; init; } = 1;
}

[MessagePackObject]
public class WorkerEventLog
{
    public DateTime EventTimestamp { get; init; }

    public required string EventMessage { get; init; } = string.Empty;

    public List<EventLogEntry> Logs { get; init; } = [];
}

[MessagePackObject]
public class WorkerEventLogCollection
{
    public List<WorkerEventLog> Events { get; init; } = [];
}