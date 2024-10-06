namespace Common.Models;

public class WorkerEvent : WorkerOut
{
    public required WorkerEventType EventType { get; set; }
}