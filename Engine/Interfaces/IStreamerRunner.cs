using Common.DTOs;

namespace Engine.Interfaces;

public interface IStreamerRunner
{
    string WorkerId { get; set; }
    Func<WorkerState, Task>? StateChangedAsync { get; set; } // Async event for state changes

    Task<(WorkerState, string)> StartAsync();
    Task<(WorkerState, string)> StopAsync();

    event EventHandler<WorkerLogEntry> LogGenerated;
    event EventHandler<ImageData> ImageGenerated;


    WorkerState GetState();
}