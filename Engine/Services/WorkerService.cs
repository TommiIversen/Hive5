using System.Runtime.InteropServices;
using Common.Models;
using Engine.Utils;

namespace Engine.Services;

public class WorkerService
{
    private readonly MessageQueue _messageQueue;
    private readonly Timer _logTimer;
    private readonly Timer _imageTimer;
    public Guid WorkerId { get; } = Guid.NewGuid();
    public bool IsRunning { get; private set; }
    private int _logCounter = 0;
    private int _logImgCounter = 0;
    private readonly ImageGenerator _generator = new();


    public WorkerService(MessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
        _logTimer = new Timer(SendLog, null, Timeout.Infinite, 300);
        _imageTimer = new Timer(SendImage, null, Timeout.Infinite, 1000);
    }

    public void Start()
    {
        IsRunning = true;
        _logTimer.Change(0, 300);  // Start log timer
        _imageTimer.Change(0, 1000);  // Start image timer
    }

    public void Stop()
    {
        IsRunning = false;
        _logTimer.Change(Timeout.Infinite, Timeout.Infinite);  // Stop log timer
        _imageTimer.Change(Timeout.Infinite, Timeout.Infinite);  // Stop image timer
    }

    private void SendLog(object? state)
    {
        if (!IsRunning) return;
            
        var counter = _logCounter++;
        var log = new LogEntry
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            Message = $"{counter} Log message at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}",
            LogSequenceNumber = counter
        };
        _messageQueue.EnqueueMessage(log);
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
        _messageQueue.EnqueueMessage(imageData);
    }

    private byte[] GenerateFakeImage()
    {
        // fake image data (placeholder)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return _generator.GenerateImageWithNumber(_logImgCounter++);
        return [0, 0, 0];
    }
}