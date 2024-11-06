namespace Common.DTOs.Events;

public class WorkerLogEntry : BaseLogEntry
{
    public required string WorkerId { get; init; }
}