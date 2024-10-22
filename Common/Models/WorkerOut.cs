namespace Common.Models;

public enum WorkerEventType
{
    Created,
    Updated,
    Deleted
}

public class WorkerOut : BaseMessage
{
    public required string WorkerId { get; set; }
    public required string? Name { get; set; }
    public required string? Description { get; set; }
    public required string? Command { get; set; }
    public required bool IsEnabled { get; set; }
    public required int WatchdogEventCount { get; set; }
    public WorkerState State { get; set; }
}

public class WorkerEvent : WorkerOut
{
    public required WorkerEventType EventType { get; set; }
}