namespace Common.DTOs;

public class WorkerEnableDisableMessage : WorkerOperationMessage
{
    public bool Enable { get; init; }
}