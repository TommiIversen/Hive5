using Common.DTOs.Enums;

namespace Common.DTOs;

public class BaseWorkerInfo : BaseMessage
{
    public required string WorkerId { get; init; }
    public required string? Name { get; init; }
    public required string? Description { get; init; }
    public required string? Command { get; init; }
    public required bool IsEnabled { get; init; }
    public required int WatchdogEventCount { get; init; }
    public WorkerState State { get; init; }
    public bool ImgWatchdogEnabled { get; init; } = true;
    public required TimeSpan ImgWatchdogGraceTime { get; init; }
    public required TimeSpan ImgWatchdogInterval { get; init; }

    public required string Streamer { get; init; }
}