namespace Common.Models;

public class WorkerEdit : WorkerOperationMessage
{
    public string? Name { get; }
    public string? Description { get; set; }
    public string? Command { get; set; }
}