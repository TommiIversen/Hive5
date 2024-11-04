namespace Common.DTOs;

public class WorkerCreateAndEdit : WorkerOperationMessage
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Command { get; set; }
    public required bool IsEnabled { get; set; }

    public bool ImgWatchdogEnabled { get; set; } = true;
    public int ImgWatchdogTimeout { get; set; } = 60;
    public required TimeSpan ImgWatchdogGraceTime { get; set; } = TimeSpan.FromSeconds(10);
    public required TimeSpan ImgWatchdogInterval { get; set; } = TimeSpan.FromSeconds(10);
}