namespace Common.DTOs.Commands;

public class WorkerEnableDisableMessage : WorkerOperationMessage
{
    public bool Enable { get; init; }
}