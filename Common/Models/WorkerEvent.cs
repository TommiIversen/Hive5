namespace Common.Models;

public class WorkerEvent : WorkerOut
{
    public required EventType EventType { get; set; }
}