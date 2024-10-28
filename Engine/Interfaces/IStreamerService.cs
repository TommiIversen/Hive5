using Common.DTOs;

namespace Engine.Interfaces;

public interface IStreamerService
{
    string WorkerId { get; set; }
    string GstCommand { get; set; }
    Func<WorkerState, Task>? StateChangedAsync { get; set; }
    Task<(WorkerState, string)> StartAsync();
    Task<(WorkerState, string)> StopAsync();
    
    event EventHandler<WorkerLogEntry> LogGenerated;
    event EventHandler<ImageData> ImageGenerated;
    WorkerState GetState();
}