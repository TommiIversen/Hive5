namespace Common.Models;

public class WorkerOperationMessage
{
    public required string WorkerId { get; set; }
    public required Guid EngineId { get; set; }
}


public class WorkerEnableDisableMessage : WorkerOperationMessage
{
    public bool Enable { get; set; }
}