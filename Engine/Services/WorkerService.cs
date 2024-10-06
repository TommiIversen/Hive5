using Common.Models;
using Engine.Interfaces;
using Engine.Utils;
using Serilog;

namespace Engine.Services;

public class WorkerService
{
    private readonly MessageQueue _messageQueue;
    private readonly IStreamerRunner _streamerRunner;

    public string WorkerId { set; get; }

    public WorkerService(MessageQueue messageQueue, IStreamerRunner streamerRunner, string workerCreateWorkerId)
    {
        _messageQueue = messageQueue;
        _streamerRunner = streamerRunner;
        _streamerRunner.WorkerId = workerCreateWorkerId;
        WorkerId = workerCreateWorkerId;

        _streamerRunner.LogGenerated += OnLogGenerated;
        _streamerRunner.ImageGenerated += OnImageGenerated;
    }

    public async Task<CommandResult> StartAsync()
    {
        Log.Information($"Starting worker {WorkerId}");

        int maxRetries = 3;
        int retryCount = 0;

        // Håndtering af tilstand 'Starting' eller 'Stopping'
        while ((_streamerRunner.GetState() == StreamerState.Starting ||
                _streamerRunner.GetState() == StreamerState.Stopping) && retryCount < maxRetries)
        {
            Log.Information(
                $"Worker {WorkerId} is in a transitional state ({_streamerRunner.GetState()}). Waiting... (Attempt {retryCount + 1}/{maxRetries})");
            await Task.Delay(500);
            retryCount++;
        }

        // Hvis arbejdstager stadig er i overgangstilstand efter maks forsøg, returner fejl
        if (_streamerRunner.GetState() == StreamerState.Starting ||
            _streamerRunner.GetState() == StreamerState.Stopping)
        {
            Log.Warning(
                $"Worker {WorkerId} is still in a transitional state ({_streamerRunner.GetState()}) after {maxRetries} attempts.");
            return new CommandResult(false,
                $"Worker is still in a transitional state after {maxRetries} attempts. Please try again later.");
        }

        // Hvis arbejdstageren allerede er i 'Running', returner en succesbesked
        if (_streamerRunner.GetState() == StreamerState.Running)
        {
            Log.Information($"Worker {WorkerId} is already running.");
            return new CommandResult(true, "Worker is already running");
        }

        // Forsøg at starte arbejdstageren, når den er 'Idle'
        var (state, message) = await _streamerRunner.StartAsync();
        bool success = state == StreamerState.Running;
        return new CommandResult(success, message);
    }


    public async Task<CommandResult> StopAsync()
    {
        Log.Information($"Stopping worker {WorkerId}");

        int maxRetries = 3;
        int retryCount = 0;

        // Håndtering af tilstand 'Starting' eller 'Stopping'
        while ((_streamerRunner.GetState() == StreamerState.Starting ||
                _streamerRunner.GetState() == StreamerState.Stopping) && retryCount < maxRetries)
        {
            Log.Information(
                $"Worker {WorkerId} is in a transitional state ({_streamerRunner.GetState()}). Waiting... (Attempt {retryCount + 1}/{maxRetries})");
            await Task.Delay(500);
            retryCount++;
        }

        // Hvis arbejdstager stadig er i overgangstilstand efter maks forsøg, returner fejl
        if (_streamerRunner.GetState() == StreamerState.Starting ||
            _streamerRunner.GetState() == StreamerState.Stopping)
        {
            Log.Warning(
                $"Worker {WorkerId} is still in a transitional state ({_streamerRunner.GetState()}) after {maxRetries} attempts.");
            return new CommandResult(false,
                $"Worker is still in a transitional state after {maxRetries} attempts. Please try again later.");
        }

        // Hvis arbejdstageren allerede er i 'Idle', returner en succesbesked
        if (_streamerRunner.GetState() == StreamerState.Idle)
        {
            Log.Information($"Worker {WorkerId} is already idle.");
            return new CommandResult(true, "Worker is already idle");
        }

        // Forsøg at stoppe arbejdstageren, når den er 'Running'
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