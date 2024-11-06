namespace Common.DTOs.Commands;

public class WorkerOperationMessage : BaseMessage
{
    public required string WorkerId { get; init; }
}