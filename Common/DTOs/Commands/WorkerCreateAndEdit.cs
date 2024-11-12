namespace Common.DTOs.Commands;

public class WorkerCreateAndEdit : WorkerOperationMessage
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Command { get; init; }
    public required bool IsEnabled { get; init; }
    public bool ImgWatchdogEnabled { get; init; } = true;
    public required TimeSpan ImgWatchdogGraceTime { get; init; }
    public required TimeSpan ImgWatchdogInterval { get; init; }
    public required string StreamerType { get; init; }
}