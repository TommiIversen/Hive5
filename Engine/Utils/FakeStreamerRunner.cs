using System.Runtime.InteropServices;
using Common.Models;
using Engine.Interfaces;

namespace Engine.Utils;

public class FakeStreamerRunner : IStreamerRunner
{
    private readonly Timer _logTimer;
    private readonly Timer _imageTimer;
    private int _logCounter = 0;
    private int _imageCounter = 0;
    public Guid WorkerId { get; } = Guid.NewGuid();
    public bool IsRunning { get; private set; }
    private readonly ImageGenerator _generator = new();

    public event EventHandler<LogEntry>? LogGenerated;
    public event EventHandler<ImageData>? ImageGenerated;

    public FakeStreamerRunner()
    {
        _logTimer = new Timer(SendLog, null, Timeout.Infinite, 300);
        _imageTimer = new Timer(SendImage, null, Timeout.Infinite, 1000);
    }

    public void Start()
    {
        IsRunning = true;
        _logTimer.Change(0, 300);
        _imageTimer.Change(0, 1000);
    }

    public void Stop()
    {
        IsRunning = false;
        _logTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _imageTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void SendLog(object? state)
    {
        if (!IsRunning) return;

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
        if (!IsRunning) return;

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
        // fake image data (placeholder)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return _generator.GenerateImageWithNumber(_imageCounter++);
        return [0, 0, 0];
    }
}