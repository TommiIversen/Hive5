using Engine.Utils;

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
    public string Name { get; set; }
    public string Description { get; set; }
    public string Command { get; set; }
    public bool Enabled { get; set; }
    public WorkerState State { get; set; }
}

public class WorkerEvent : WorkerOut
{
    public required WorkerEventType EventType { get; set; }
}