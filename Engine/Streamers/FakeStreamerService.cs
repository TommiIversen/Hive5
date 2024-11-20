using System.Runtime.InteropServices;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.Attributes;
using Engine.Interfaces;
using Engine.Utils;

namespace Engine.Streamers;

[FriendlyName("FakeStreamer")]
public class FakeStreamerService : IStreamerService
{
    private readonly ImageGenerator _generator = new();
    private int _imageCounter;
    private CancellationTokenSource? _imageTaskCts;
    private bool _isPauseActive;
    private CancellationTokenSource? _logTaskCts;
    private WorkerState _state = WorkerState.Idle;

    public required string WorkerId { get; set; }
    public required string GstCommand { get; set; }

    public Func<WorkerLogEntry, Task> LogCallback { get; set; } = async _ => { };
    public Func<WorkerImageData, Task> ImageCallback { get; set; } = async _ => { };
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
            await CreateAndSendLog(msg);
            return (_state, msg);
        }

        _state = WorkerState.Starting;
        await OnStateChangedAsync(_state); // Trigger state change

        msg = $"Starting streamer... with command: {GstCommand}";
        await CreateAndSendLog(msg);

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund
        _imageCounter = 0;

        // Start asynkrone loops
        _logTaskCts = new CancellationTokenSource();
        _imageTaskCts = new CancellationTokenSource();

        _ = StartLogLoopAsync(_logTaskCts.Token);
        _ = StartImageLoopAsync(_imageTaskCts.Token);

        _state = WorkerState.Running;
        await OnStateChangedAsync(_state);

        msg = "Streamer started successfully.";
        await CreateAndSendLog(msg);
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
        await CreateAndSendLog("Streamer stopping", LogLevel.Critical);

        Console.WriteLine("Stopping streamer...");

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund

        // Stop asynkrone loops
        _logTaskCts?.Cancel();
        _imageTaskCts?.Cancel();
        _logTaskCts = null;
        _imageTaskCts = null;

        _state = WorkerState.Idle;
        await OnStateChangedAsync(_state); // Trigger state change

        Console.WriteLine("Streamer stopped.");
        return (_state, "Streamer stopped successfully.");
    }

    public WorkerState GetState()
    {
        return _state;
    }

    private async Task StartLogLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await AutoLog();
                await Task.Delay(300, token); // Log hvert 300 ms
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Log loop canceled.");
        }
    }

    private async Task StartImageLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await SendImage();
                await Task.Delay(1000, token); // Billede hvert sekund
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Image loop canceled.");
        }
    }

    private async Task AutoLog()
    {
        if (_state != WorkerState.Running) return;

        await CreateAndSendLog("Fake log message");
    }

    private async Task SendImage()
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
            await ImageCallback(imageData);
            await CreateAndSendLog("Streamer paused for 4 seconds", LogLevel.Warning);
            await Task.Delay(4000); // Pause i 4 sekunder
            _isPauseActive = false;
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
            await ImageCallback(imageData);
        }
    }

    private byte[] GenerateFakeImage(string text = "")
    {
        // Fake image data (placeholder)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return _generator.GenerateImageWithNumber(_imageCounter++, $"FAKE-{text}");
        return new byte[] { 0, 0, 0 };
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