namespace Common.DTOs;

public class WorkerEvent : WorkerOut
{
    public required EventType EventType { get; set; }
}