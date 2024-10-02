using Common.Models;
using Engine.Interfaces;

namespace Engine.Services;

public class WorkerService
{
    private readonly MessageQueue _messageQueue;
    private readonly IStreamerRunner _streamerRunner;

    public Guid WorkerId => _streamerRunner.WorkerId;
    public bool IsRunning => _streamerRunner.IsRunning;

    public WorkerService(MessageQueue messageQueue, IStreamerRunner streamerRunner)
    {
        _messageQueue = messageQueue;
        _streamerRunner = streamerRunner;

        _streamerRunner.LogGenerated += OnLogGenerated;
        _streamerRunner.ImageGenerated += OnImageGenerated;
    }

    public void Start()
    {
        _streamerRunner.Start();
    }

    public void Stop()
    {
        _streamerRunner.Stop();
    }

    private void OnLogGenerated(object? sender, LogEntry log)
    {
        _messageQueue.EnqueueMessage(log);
    }

    private void OnImageGenerated(object? sender, ImageData image)
    {
        _messageQueue.EnqueueMessage(image);
    }
}