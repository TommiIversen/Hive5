using System.Runtime.InteropServices;
using Common.Models;
using Engine.Interfaces;

namespace Engine.Utils;

public partial class FakeStreamerRunner : IStreamerRunner
{
    private readonly Timer _logTimer;
    private readonly Timer _imageTimer;
    private int _logCounter = 0;
    private int _imageCounter = 0;
    public Guid WorkerId { get; } = Guid.NewGuid();
    public bool IsRunning { get; }

    private readonly ImageGenerator _generator = new();
    private StreamerState _state = StreamerState.Idle;

    public event EventHandler<LogEntry>? LogGenerated;
    public event EventHandler<ImageData>? ImageGenerated;

    public FakeStreamerRunner()
    {
        _logTimer = new Timer(SendLog, null, Timeout.Infinite, 300);
        _imageTimer = new Timer(SendImage, null, Timeout.Infinite, 1000);
    }

    public async Task<(StreamerState, string)> StartAsync()
    {
        if (_state == StreamerState.Running || _state == StreamerState.Starting)
        {
            return (_state, "Streamer is already running or starting.");
        }

        if (_state == StreamerState.Stopping)
        {
            return (_state, "Streamer is currently stopping. Please wait.");
        }

        _state = StreamerState.Starting;
        Console.WriteLine("Starting streamer...");

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund

        _logTimer.Change(0, 300);
        _imageTimer.Change(0, 1000);
        _state = StreamerState.Running;

        Console.WriteLine("Streamer started.");
        return (_state, "Streamer started successfully.");
    }

    public async Task<(StreamerState, string)> StopAsync()
    {
        if (_state == StreamerState.Idle || _state == StreamerState.Stopping)
        {
            return (_state, "Streamer is not running or is already stopping.");
        }

        if (_state == StreamerState.Starting)
        {
            return (_state, "Streamer is starting. Please wait.");
        }

        _state = StreamerState.Stopping;
        Console.WriteLine("Stopping streamer...");

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund

        _logTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _imageTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _state = StreamerState.Idle;

        Console.WriteLine("Streamer stopped.");
        return (_state, "Streamer stopped successfully.");
    }

    private void SendLog(object? state)
    {
        if (_state != StreamerState.Running) return;

        var log = new LogEntry
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            Message = $"{_logCounter} Fake log message",
            LogSequenceNumber = _logCounter++
        };
        LogGenerated?.Invoke(this, log);
    }

    private void SendImage(object? state)
    {
        if (_state != StreamerState.Running) return;

        var imageData = new ImageData
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            ImageBytes = GenerateFakeImage()
        };
        ImageGenerated?.Invoke(this, imageData);
    }

    private byte[] GenerateFakeImage()
    {
        // Fake image data (placeholder)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return _generator.GenerateImageWithNumber(_imageCounter++);
        return new byte[] { 0, 0, 0 };
    }

    public StreamerState GetState()
    {
        return _state;
    }
}