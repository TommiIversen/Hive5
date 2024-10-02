using Common.Models;

namespace Engine.Interfaces;

public interface IStreamerRunner
{
    Guid WorkerId { get; }
    bool IsRunning { get; }

    void Start();
    void Stop();
    event EventHandler<LogEntry> LogGenerated;
    event EventHandler<ImageData> ImageGenerated;
}