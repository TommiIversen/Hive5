using Common.Models;
using Engine.Interfaces;
using Engine.Utils;

namespace Engine.Services;

public class WorkerService
{
    private readonly MessageQueue _messageQueue;
    private readonly IStreamerRunner _streamerRunner;

    public Guid WorkerId => _streamerRunner.WorkerId;

    public WorkerService(MessageQueue messageQueue, IStreamerRunner streamerRunner)
    {
        _messageQueue = messageQueue;
        _streamerRunner = streamerRunner;

        _streamerRunner.LogGenerated += OnLogGenerated;
        _streamerRunner.ImageGenerated += OnImageGenerated;
    }

    public async Task<CommandResult> StartAsync()
    {
        var (state, message) = await _streamerRunner.StartAsync();
        bool success = state == StreamerState.Running;
        return new CommandResult(success, message);
    }

    public async Task<CommandResult> StopAsync()
    {
        var (state, message) = await _streamerRunner.StopAsync();
        bool success = state == StreamerState.Idle;
        return new CommandResult(success, message);
    }

    private void OnLogGenerated(object? sender, LogEntry log)
    {
        _messageQueue.EnqueueMessage(log);
    }

    private void OnImageGenerated(object? sender, ImageData image)
    {
        _messageQueue.EnqueueMessage(image);
    }

    public StreamerState GetState()
    {
        return _streamerRunner.GetState();
    }
}