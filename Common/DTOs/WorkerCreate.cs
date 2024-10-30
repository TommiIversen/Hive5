namespace Common.DTOs;

public class WorkerCreate : WorkerOperationMessage
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Command { get; set; }
    public bool ImgWatchdogEnabled { get; set; } = true;
}