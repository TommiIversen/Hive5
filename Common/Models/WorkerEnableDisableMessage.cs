namespace Common.Models;

public class WorkerEnableDisableMessage : WorkerOperationMessage
{
    public bool Enable { get; set; }
}