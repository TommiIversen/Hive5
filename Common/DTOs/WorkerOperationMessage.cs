namespace Common.DTOs;

public class WorkerOperationMessage
{
    public required string WorkerId { get; set; }
    public required Guid EngineId { get; set; }
}