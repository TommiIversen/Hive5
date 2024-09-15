using Engine.Services;
using StreamHub.Models;

namespace Engine.Models;

public class Worker
{
    private readonly StreamHubService _streamHubService;
    private readonly Timer _logTimer;
    private readonly Timer _imageTimer;
    public Guid WorkerId { get; } = Guid.NewGuid();
    private bool _isRunning;

    public Worker(StreamHubService streamHubService)
    {
        Console.WriteLine("Worker created");
        _streamHubService = streamHubService;
        _logTimer = new Timer(SendLog, null, Timeout.Infinite, 1000);
        _imageTimer = new Timer(SendImage, null, Timeout.Infinite, 1000);
    }

    public void Start()
    {
        _isRunning = true;
        _logTimer.Change(0, 1000);
        _imageTimer.Change(0, 1000);
    }

    public void Stop()
    {
        _isRunning = false;
        _logTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _imageTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private async void SendLog(object state)
    {
        if (_isRunning)
        {
            var log = new LogEntry
            {
                EngineId = _streamHubService.EngineId,
                WorkerId = WorkerId,
                Timestamp = DateTime.UtcNow,
                Message = $"Log message at {DateTime.UtcNow}"
            };
            await _streamHubService.SendLogAsync(log);
        }
    }

    private async void SendImage(object state)
    {
        if (_isRunning)
        {
            var imageData = new ImageData
            {
                EngineId = _streamHubService.EngineId,
                WorkerId = WorkerId,
                Timestamp = DateTime.UtcNow,
                ImageBytes = GenerateFakeImage()
            };
            await _streamHubService.SendImageAsync(imageData);
        }
    }

    private byte[] GenerateFakeImage()
    {
        // Generer en lille fake billede som byte array
        return new byte[100]; // Placeholder for billeddata
    }
}