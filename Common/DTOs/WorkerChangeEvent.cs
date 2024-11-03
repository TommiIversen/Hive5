namespace Common.DTOs;

public class WorkerChangeEvent : WorkerOut
{
    public required EventType EventType { get; set; }
}