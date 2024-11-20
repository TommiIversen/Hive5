using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.Attributes;
using Engine.Interfaces;

namespace Engine.Streamers;

[FriendlyName("GstStreamer")]
public class GstStreamerService : IStreamerService
{
    private readonly GStreamerProcessHandler _handler;
    private WorkerState _state = WorkerState.Idle;

    public GstStreamerService()
    {
        _handler = new GStreamerProcessHandler(LogCallbackAsync, ImageCallbackAsync);
    }

    public required string WorkerId { get; set; }
    public required string GstCommand { get; set; }
    public Func<WorkerLogEntry, Task> LogCallback { get; set; }
    public Func<WorkerImageData, Task> ImageCallback { get; set; }
    public Func<WorkerState, Task>? StateChangedAsync { get; set; }

    public async Task<(WorkerState, string)> StartAsync()
    {
        string msg;

        if (_state == WorkerState.Running || _state == WorkerState.Starting)
        {
            msg = "Streamer is already running or starting.";
            await CreateAndSendLog(msg);
            return (_state, msg);
        }

        if (_state == WorkerState.Stopping)
        {
            msg = "Streamer is currently stopping. Please wait.";
            await SendLog(msg);
            return (_state, msg);
        }

        _state = WorkerState.Starting;
        await OnStateChangedAsync(_state); // Trigger state change

        msg = $"Starting streamer... with command: {GstCommand}";
        await SendLog(msg);

        await _handler.StartGStreamerProcessAsync(GstCommand, CancellationToken.None);

        _state = WorkerState.Running;
        await OnStateChangedAsync(_state);

        msg = "Streamer started successfully.";
        await SendLog(msg);
        return (_state, msg);
    }

    public async Task<(WorkerState, string)> StopAsync()
    {
        switch (_state)
        {
            case WorkerState.Idle or WorkerState.Stopping:
                return (_state, "Streamer is not running or is already stopping.");
            case WorkerState.Starting:
                return (_state, "Streamer is starting. Please wait.");
        }


        await CreateAndSendLog("Streamer stopping", LogLevel.Critical);
        _state = WorkerState.Stopping;
        await OnStateChangedAsync(_state); // Trigger state change
        _handler.StopGStreamerProcess();

        Console.WriteLine("Stopping streamer...");

        _state = WorkerState.Idle;
        await OnStateChangedAsync(_state); // Trigger state change

        Console.WriteLine("Streamer stopped.");
        return (_state, "Streamer stopped successfully.");
    }


    public WorkerState GetState()
    {
        return _state;
    }


    private async Task LogCallbackAsync(string message)
    {
        Console.WriteLine($"----------Log: {message}");
        await CreateAndSendLog(message);
    }

    private async Task ImageCallbackAsync(byte[] imageData)
    {
        var gstImageData = new WorkerImageData
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            ImageBytes = imageData
        };
        await ImageCallback.Invoke(gstImageData);
    }


    private async Task SendLog(string logMsg)
    {
        await CreateAndSendLog(logMsg);
    }


    private async Task CreateAndSendLog(string message, LogLevel logLevel = LogLevel.Information)
    {
        var log = new WorkerLogEntry
        {
            WorkerId = WorkerId,
            LogTimestamp = DateTime.UtcNow,
            Message = message,
            LogLevel = logLevel
        };
        await LogCallback.Invoke(log);
    }


    private async Task OnStateChangedAsync(WorkerState newState)
    {
        if (StateChangedAsync != null) await StateChangedAsync.Invoke(newState);
    }
}