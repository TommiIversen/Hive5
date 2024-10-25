using Common.DTOs;

namespace Engine.Interfaces;

public interface IStreamerRunner
{
    string WorkerId { get; set; }
    
    Task<(WorkerState, string)> StartAsync();
    Task<(WorkerState, string)> StopAsync();

    event EventHandler<WorkerLogEntry> LogGenerated;
    event EventHandler<ImageData> ImageGenerated;
    Func<WorkerState, Task>? StateChangedAsync { get; set; } // Async event for state changes


    WorkerState GetState();
}