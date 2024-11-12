using System.Runtime.InteropServices;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.Attributes;
using Engine.Interfaces;
using Engine.Utils;

namespace Engine.Streamers;


[FriendlyName("GstStreamer")]

public class GstStreamerService : IStreamerService
{
    private readonly ImageGenerator _generator = new();
    private readonly Timer _imageTimer;
    private readonly Timer _logTimer;
    private int _imageCounter;
    private bool _isPauseActive;
    private WorkerState _state = WorkerState.Idle;


    public GstStreamerService()
    {
        _logTimer = new Timer(AutoLog, null, Timeout.Infinite, 300);
        _imageTimer = new Timer(SendImage, null, Timeout.Infinite, 1000);
    }

    public required string WorkerId { get; set; }
    public required string GstCommand { get; set; }

    public event EventHandler<WorkerLogEntry>? LogGenerated;
    public event EventHandler<WorkerImageData>? ImageGenerated;
    public Func<WorkerState, Task>? StateChangedAsync { get; set; }


    public async Task<(WorkerState, string)> StartAsync()
    {
        string msg;

        if (_state == WorkerState.Running || _state == WorkerState.Starting)
        {
            msg = "Streamer is already running or starting.";
            SendLog(msg);
            return (_state, msg);
        }

        if (_state == WorkerState.Stopping)
        {
            msg = "Streamer is currently stopping. Please wait.";
            SendLog(msg);
            return (_state, msg);
        }

        _state = WorkerState.Starting;
        await OnStateChangedAsync(_state); // Trigger state change

        msg = $"Starting streamer... with command: {GstCommand}";
        SendLog(msg);

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund
        _imageCounter = 0;

        _logTimer.Change(0, 300);
        _imageTimer.Change(0, 1000);

        _state = WorkerState.Running;
        await OnStateChangedAsync(_state);

        msg = "Streamer started successfully.";
        SendLog(msg);
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

        _state = WorkerState.Stopping;
        await OnStateChangedAsync(_state); // Trigger state change
        CreateAndSendLog("Streamer stopping", LogLevel.Critical);


        Console.WriteLine("Stopping streamer...");

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund

        _logTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _imageTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _state = WorkerState.Idle;
        await OnStateChangedAsync(_state); // Trigger state change

        Console.WriteLine("Streamer stopped.");
        return (_state, "Streamer stopped successfully.");
    }

    public WorkerState GetState()
    {
        return _state;
    }


    private void SendLog(string logMsg)
    {
        CreateAndSendLog(logMsg);
    }

    private void AutoLog(object? state)
    {
        if (_state != WorkerState.Running) return;

        CreateAndSendLog("Fake log message");
    }

    private void CreateAndSendLog(string message, LogLevel logLevel = LogLevel.Information)
    {
        var log = new WorkerLogEntry
        {
            WorkerId = WorkerId,
            LogTimestamp = DateTime.UtcNow,
            Message = message,
            LogLevel = logLevel
        };
        LogGenerated?.Invoke(this, log);
    }


    private void SendImage(object? state)
    {
        if (_state != WorkerState.Running) return;

        // Check for pause hver 30. billede
        if (_imageCounter != 0 && _imageCounter % 30 == 0 && !_isPauseActive)
        {
            _isPauseActive = true;
            var imageData = new WorkerImageData
            {
                WorkerId = WorkerId,
                Timestamp = DateTime.UtcNow,
                ImageBytes = GenerateFakeImage("Crashed")
            };
            ImageGenerated?.Invoke(this, imageData);
            CreateAndSendLog("Streamer paused for 4 seconds", LogLevel.Warning);
            Task.Delay(4000).ContinueWith(_ => { _isPauseActive = false; });
            return; // Skip image generation under pause
        }

        if (!_isPauseActive)
        {
            var imageData = new WorkerImageData
            {
                WorkerId = WorkerId,
                Timestamp = DateTime.UtcNow,
                ImageBytes = GenerateFakeImage(WorkerId)
            };
            ImageGenerated?.Invoke(this, imageData);
        }
    }

    private byte[] GenerateFakeImage(string text = "")
    {
        // Fake image data (placeholder)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return _generator.GenerateImageWithNumber(_imageCounter++, $"GST-{text}");
        return new byte[] { 0, 0, 0 };
    }

    private async Task OnStateChangedAsync(WorkerState newState)
    {
        if (StateChangedAsync != null) await StateChangedAsync.Invoke(newState);
    }
}