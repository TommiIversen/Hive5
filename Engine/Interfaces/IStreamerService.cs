using Common.DTOs.Enums;
using Common.DTOs.Events;

namespace Engine.Interfaces;

public interface IStreamerService
{
    string WorkerId { get; set; }
    string GstCommand { get; set; }
    Func<WorkerState, Task>? StateChangedAsync { get; set; }
    Task<(WorkerState, string)> StartAsync();
    Task<(WorkerState, string)> StopAsync();
    // Erstat events med Func-delegeringer
    Func<WorkerLogEntry, Task>? LogCallback { get; set; }
    Func<WorkerImageData, Task>? ImageCallback { get; set; }

    //event EventHandler<WorkerLogEntry> LogGenerated;
    //event EventHandler<WorkerImageData> ImageGenerated;
    WorkerState GetState();
}