﻿using Common.DTOs.Commands;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Models;
using Serilog;

namespace Engine.Services;

public interface IWorkerService
{
    string WorkerId { get; }
    Task<CommandResult> StartAsync();
    Task<CommandResult> StopAsync();
    WorkerState GetState();
    void SetGstCommand(string gstCommand);
    void UpdateWatchdogSettingsAsync(bool isEnabled, TimeSpan interval, TimeSpan graceTime);
    void ReplaceStreamer(IStreamerService newStreamer);
}

public class WorkerService : IWorkerService
{
    private readonly ILoggerService _loggerService;
    private readonly IMessageQueue _messageQueue;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IStreamerWatchdogService _watchdogService;
    private WorkerState _desiredState;
    private int _imageCounter;
    private DateTime _lastImageUpdate;
    private IStreamerService _streamerService;

    public WorkerService(
        ILoggerService loggerService,
        IMessageQueue messageQueue,
        IStreamerService streamerService,
        IRepositoryFactory repositoryFactory,
        IStreamerWatchdogFactory watchdogFactory,
        WorkerConfiguration config)
    {
        _messageQueue = messageQueue;
        _streamerService = streamerService;
        _streamerService.WorkerId = config.WorkerId;
        _repositoryFactory = repositoryFactory;
        _loggerService = loggerService;

        //WorkerId = workerCreateWorkerId;
        WorkerId = config.WorkerId;

        // Forbind runnerens events til WorkerService handlers
        _streamerService.GstCommand = config.GstCommand;
        _streamerService.LogCallback = async logEntry => await OnLogGenerated(logEntry);
        _streamerService.ImageCallback = async imageData => await OnImageGenerated(imageData);
        _streamerService.StateChangedAsync = async newState => { await HandleStateChangeAsync(newState); };

        // Initialiser watchdog med callbacks
        _watchdogService = watchdogFactory.CreateWatchdog(
            config.WorkerId,
            ShouldRestart,
            WatchdogRestartCallback,
            config.ImgWatchdogGraceTime,
            config.ImgWatchdogInterval);
        _watchdogService.SetEnabled(config.ImgWatchdogEnabled);
        _watchdogService.StateChanged += async (sender, message) => await OnWatchdogStateChanged(sender, message);

        _desiredState = WorkerState.Idle;
    }

    public string WorkerId { set; get; }

    public void UpdateWatchdogSettingsAsync(bool isEnabled, TimeSpan interval, TimeSpan graceTime)
    {
        _watchdogService.SetEnabled(isEnabled);
        _watchdogService.UpdateCheckInterval(interval);
        _watchdogService.UpdateGraceTime(graceTime);
    }


    public void SetGstCommand(string gstCommand)
    {
        _streamerService.GstCommand = gstCommand;
    }

    public async Task<CommandResult> StartAsync()
    {
        LogInfo($"Starting worker {WorkerId}");
        _desiredState = WorkerState.Running;

        var maxRetries = 3;
        var retryCount = 0;

        // Håndtering af tilstand 'Starting', 'Stopping' eller 'Restarting'
        while ((_streamerService.GetState() == WorkerState.Starting ||
                _streamerService.GetState() == WorkerState.Stopping ||
                _streamerService.GetState() == WorkerState.Restarting) && retryCount < maxRetries)
        {
            LogInfo(
                $"Worker {WorkerId} is in a transitional state ({_streamerService.GetState()}). Waiting... (Attempt {retryCount + 1}/{maxRetries})",
                LogLevel.Warning);
            await Task.Delay(500);
            retryCount++;
        }

        if (_streamerService.GetState() == WorkerState.Starting ||
            _streamerService.GetState() == WorkerState.Stopping ||
            _streamerService.GetState() == WorkerState.Restarting)
        {
            LogInfo(
                $"Worker {WorkerId} is still in a transitional state ({_streamerService.GetState()}) after {maxRetries} attempts.",
                LogLevel.Error);

            return new CommandResult(false,
                $"Worker is still in a transitional state after {maxRetries} attempts. Please try again later.");
        }

        if (_streamerService.GetState() == WorkerState.Running)
        {
            LogInfo($"Worker {WorkerId} is already running.", LogLevel.Warning);

            return new CommandResult(true, "Worker is already running");
        }

        var (state, message) = await _streamerService.StartAsync();
        var success = state == WorkerState.Running;

        // Start watchdog hvis streameren er startet korrekt
        await _watchdogService.StartAsync();
        return new CommandResult(success, message);
    }


    public async Task<CommandResult> StopAsync()
    {
        LogInfo($"Stopping worker {WorkerId}");

        _desiredState = WorkerState.Idle;

        var maxRetries = 3;
        var retryCount = 0;

        // Håndtering af tilstand 'Starting', 'Stopping' eller 'Restarting'
        while ((_streamerService.GetState() == WorkerState.Starting ||
                _streamerService.GetState() == WorkerState.Stopping ||
                _streamerService.GetState() == WorkerState.Restarting) && retryCount < maxRetries)
        {
            LogInfo(
                $"Worker {WorkerId} is in a transitional state ({_streamerService.GetState()}). Waiting... (Attempt {retryCount + 1}/{maxRetries})",
                LogLevel.Warning);
            await Task.Delay(500);
            retryCount++;
        }

        if (_streamerService.GetState() == WorkerState.Starting ||
            _streamerService.GetState() == WorkerState.Stopping ||
            _streamerService.GetState() == WorkerState.Restarting)
        {
            LogInfo(
                $"Worker {WorkerId} is still in a transitional state ({_streamerService.GetState()}) after {maxRetries} attempts.",
                LogLevel.Error);

            return new CommandResult(false,
                $"Worker is still in a transitional state after {maxRetries} attempts. Please try again later.");
        }

        if (_streamerService.GetState() == WorkerState.Idle)
        {
            LogInfo($"Worker {WorkerId} is already idle.", LogLevel.Warning);
            return new CommandResult(true, "Worker is already idle");
        }

        // Stop watchdog før streameren stoppes
        LogInfo("Stopping watchdog");
        await _watchdogService.StopAsync();
        LogInfo("Watchdog stopped");
        var (state, message) = await _streamerService.StopAsync();
        var success = state == WorkerState.Idle;
        return new CommandResult(success, message);
    }

    public WorkerState GetState()
    {
        return _streamerService.GetState();
    }

    public void ReplaceStreamer(IStreamerService newStreamer)
    {
        // Unbind callbacks from the old streamer
        _streamerService.LogCallback = null;
        _streamerService.ImageCallback = null;
        _streamerService.StateChangedAsync = null;

        // Replace the streamer
        _streamerService = newStreamer;

        // Initialize new streamer's properties and callbacks
        _streamerService.WorkerId = WorkerId;
        _streamerService.GstCommand = _streamerService.GstCommand;
        _streamerService.LogCallback = async logEntry => await OnLogGenerated(logEntry);
        _streamerService.ImageCallback = async imageData => await OnImageGenerated(imageData);
        _streamerService.StateChangedAsync = async newState => await HandleStateChangeAsync(newState);

        LogInfo($"Streamer replaced for worker {WorkerId}");
    }

    public async Task HandleStateChangeAsync(WorkerState newState)
    {
        await HandleStateChange(this, newState, ChangeEventType.Updated, "State changed in runner");
    }


    private (bool, string) ShouldRestart()
    {
        // Hvis vi er i gang med en planlagt eller brugerinitieret stopproces, eller hvis vi er i gang med en genstart, skal watchdog ikke genstarte
        if (_desiredState == WorkerState.Idle || _streamerService.GetState() == WorkerState.Restarting)
            return (false, "Worker is expected to be idle or is already restarting.");

        // Brug den faktiske state fra _streamerRunner til at afgøre, om en genstart er nødvendig
        var isRunning = _streamerService.GetState() == WorkerState.Running;
        var timeSinceLastImage = (DateTime.UtcNow - _lastImageUpdate).TotalSeconds;

        if (!isRunning) return (true, "Process not running");

        if (timeSinceLastImage > 2) return (true, "Image update missing");

        return (false, string.Empty);
    }

    public Task WatchdogRestartCallback(string reason)
    {
        _ = Task.Run(async () => await RestartWorkerAsync(reason));
        return Task.CompletedTask;
    }


    public async Task RestartWorkerAsync(string reason)
    {
        var logMessage = $"Restarting worker {WorkerId} due to: {reason}";
        LogInfo(logMessage, LogLevel.Error);

        // Sæt desired state til Restarting
        _desiredState = WorkerState.Restarting;

        // Stop workeren først
        var stopResult = await StopAsync();
        if (!stopResult.Success)
        {
            logMessage = $"Failed to stop worker {WorkerId} during restart. Reason: {stopResult.Message}";
            LogInfo(logMessage, LogLevel.Error);
            return;
        }

        // Start workeren igen
        var startResult = await StartAsync();
        if (!startResult.Success)
        {
            logMessage = $"Failed to start worker {WorkerId} during restart. Reason: {startResult.Message}";
            LogInfo(logMessage, LogLevel.Error);
            _desiredState = WorkerState.Idle;
        }
    }

    private async Task OnWatchdogStateChanged(object? sender, string message)
    {
        LogInfo(message, LogLevel.Error);

        var workerRepository = _repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(WorkerId);

        if (workerEntity != null)
        {
            // Tæl op
            workerEntity.WatchdogEventCount++;
            await workerRepository.UpdateWorkerAsync(workerEntity); // Gem ændringerne asynkront

            // Hent de sidste 20 logs og gem dem som en hændelse
            var logs = _loggerService.GetLastWorkerLogs(WorkerId).Take(20).ToList();
            await workerRepository.AddWorkerEventAsync(WorkerId, message, logs);

            LogInfo(
                $"OnWatchdogStateChanged: Watchdog state changed for worker {WorkerId}. Event count: {workerEntity.WatchdogEventCount}");
        }
        else
        {
            LogInfo($"OnWatchdogStateChanged: Worker with ID {WorkerId} not found.", LogLevel.Error);
        }
    }

    private async Task OnLogGenerated(WorkerLogEntry workerLog)
    {
        _loggerService.LogMessage(workerLog);
    }

    private async Task OnImageGenerated(WorkerImageData workerImage)
    {
        workerImage.ImageSequenceNumber = Interlocked.Increment(ref _imageCounter);
        _messageQueue.EnqueueMessage(workerImage);
        _lastImageUpdate = workerImage.Timestamp;
    }

    public void LogInfo(string message, LogLevel logLevel = LogLevel.Information)
    {
        _loggerService.LogMessage(new WorkerLogEntry
        {
            WorkerId = WorkerId,
            Message = $"WorkerService: {message}",
            LogLevel = logLevel,
            LogTimestamp = DateTime.UtcNow
        });
    }

    private async Task HandleStateChange(WorkerService workerService, WorkerState newState,
        ChangeEventType changeEventType = ChangeEventType.Updated, string reason = "")
    {
        var logMessage = $"Worker {workerService.WorkerId} state changed to {newState}: {reason}";
        Log.Information(logMessage);

        var workerRepository = _repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerService.WorkerId);

        if (workerEntity != null)
        {
            var workerEvent = workerEntity.ToWorkerEvent(newState, changeEventType);
            _messageQueue.EnqueueMessage(workerEvent);
        }
        else
        {
            LogInfo($"HandleStateChange: Worker with ID {WorkerId} not found.", LogLevel.Warning);
        }
    }
}