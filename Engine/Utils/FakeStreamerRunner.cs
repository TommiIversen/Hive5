using System.Runtime.InteropServices;
using Common.DTOs;
using Engine.Interfaces;

namespace Engine.Utils;

public class FakeStreamerRunner : IStreamerRunner
{
    private readonly Timer _logTimer;
    private readonly Timer _imageTimer;
    private int _imageCounter = 0;
    private bool _isPauseActive = false;

    public required string WorkerId { get; set; }

    private readonly ImageGenerator _generator = new();
    private WorkerState _state = WorkerState.Idle;

    public event EventHandler<LogEntry>? LogGenerated;
    public event EventHandler<ImageData>? ImageGenerated;
    public Func<WorkerState, Task>? StateChangedAsync { get; set; }


    public FakeStreamerRunner()
    {
        _logTimer = new Timer(AutoLog, null, Timeout.Infinite, 300);
        _imageTimer = new Timer(SendImage, null, Timeout.Infinite, 1000);
    }


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


        msg = "Starting streamer...";
        Console.WriteLine(msg);

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
        
        Console.WriteLine("Stopping streamer...");

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund

        _logTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _imageTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        _state = WorkerState.Idle;
        await OnStateChangedAsync(_state); // Trigger state change

        Console.WriteLine("Streamer stopped.");
        return (_state, "Streamer stopped successfully.");
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

    private void CreateAndSendLog(string message)
    {
        var log = new LogEntry
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            Message = message
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
            var imageData = new ImageData
            {
                WorkerId = WorkerId,
                Timestamp = DateTime.UtcNow,
                ImageBytes = GenerateFakeImage("Crashed")
            };
            ImageGenerated?.Invoke(this, imageData);
            SendLog(" - Streamer paused for 4 seconds");
            Task.Delay(4000).ContinueWith(_ => { _isPauseActive = false; });
            return; // Skip image generation under pause
        }

        if (!_isPauseActive)
        {
            var imageData = new ImageData
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
            return _generator.GenerateImageWithNumber(_imageCounter++, text);
        return new byte[] { 0, 0, 0 };
    }

    public WorkerState GetState()
    {
        return _state;
    }
    
    private async Task OnStateChangedAsync(WorkerState newState)
    {
        if (StateChangedAsync != null)
        {
            await StateChangedAsync.Invoke(newState);
        }
    }
}