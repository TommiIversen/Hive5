namespace Common.DTOs;

public class WorkerOperationMessage : BaseMessage
{
    public required string WorkerId { get; set; }
}