namespace Common.Models;

public class WorkerCreate : WorkerOperationMessage
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Command { get; set; }
}


public class WorkerEdit : WorkerOperationMessage
{
    public string? Name { get; }
    public string? Description { get; set; }
    public string? Command { get; set; }
}