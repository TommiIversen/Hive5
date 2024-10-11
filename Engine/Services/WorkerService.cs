using Common.Models;
using Engine.Interfaces;
using Engine.Utils;
using Serilog;

namespace Engine.Services;

public class WorkerService
{
    private readonly WorkerManager _workerManager;
    private readonly MessageQueue _messageQueue;
    private readonly IStreamerRunner _streamerRunner;

    private readonly RunnerWatchdog _watchdog;
    private StreamerState _desiredState;
    private DateTime _lastImageUpdate;

    public string WorkerId { set; get; }

    private int _logCounter = 0;

    public WorkerService(WorkerManager workerManager, MessageQueue messageQueue, IStreamerRunner streamerRunner, string workerCreateWorkerId)
    {
        _workerManager = workerManager;
        _messageQueue = messageQueue;
        _streamerRunner = streamerRunner;
        _streamerRunner.WorkerId = workerCreateWorkerId;
        WorkerId = workerCreateWorkerId;

        // Forbind runnerens events til WorkerService handlers
        _streamerRunner.LogGenerated += OnLogGenerated;
        _streamerRunner.ImageGenerated += OnImageGenerated;
        // Abonner på asynkrone state changes
        _streamerRunner.StateChangedAsync = async (newState) =>
        {
            // Asynkron logik her
            await HandleStateChangeAsync(newState);
        };


        // Initialiser watchdog med callbacks
        _watchdog = new RunnerWatchdog(workerCreateWorkerId, ShouldRestart, RestartWorker, TimeSpan.FromSeconds(6),
            TimeSpan.FromSeconds(1));
        _watchdog.StateChanged += OnWatchdogStateChanged;

        // Forbind runnerens logevent til watchdog'ens loghåndtering
        _streamerRunner.LogGenerated += _watchdog.OnRunnerLogGenerated;

        _desiredState = StreamerState.Idle;
    }
    
    private async Task HandleStateChangeAsync(StreamerState newState)
    {
        await _workerManager.HandleStateChange(this, newState, WorkerEventType.Updated, "State changed in runner");
    }


    public async Task<CommandResult> StartAsync()
    {
        Log.Information($"Starting worker {WorkerId}");
        _desiredState = StreamerState.Running;

        int maxRetries = 3;
        int retryCount = 0;

        // Håndtering af tilstand 'Starting', 'Stopping' eller 'Restarting'
        while ((_streamerRunner.GetState() == StreamerState.Starting ||
                _streamerRunner.GetState() == StreamerState.Stopping ||
                _streamerRunner.GetState() == StreamerState.Restarting) && retryCount < maxRetries)
        {
            Log.Information(
                $"Worker {WorkerId} is in a transitional state ({_streamerRunner.GetState()}). Waiting... (Attempt {retryCount + 1}/{maxRetries})");
            await Task.Delay(500);
            retryCount++;
        }

        if (_streamerRunner.GetState() == StreamerState.Starting ||
            _streamerRunner.GetState() == StreamerState.Stopping ||
            _streamerRunner.GetState() == StreamerState.Restarting)
        {
            Log.Warning(
                $"Worker {WorkerId} is still in a transitional state ({_streamerRunner.GetState()}) after {maxRetries} attempts.");
            return new CommandResult(false,
                $"Worker is still in a transitional state after {maxRetries} attempts. Please try again later.");
        }

        if (_streamerRunner.GetState() == StreamerState.Running)
        {
            Log.Information($"Worker {WorkerId} is already running.");
            return new CommandResult(true, "Worker is already running");
        }

        var (state, message) = await _streamerRunner.StartAsync();
        bool success = state == StreamerState.Running;
        
        // Start watchdog hvis streameren er startet korrekt
        await _watchdog.StartAsync();
        return new CommandResult(success, message);
    }


    public async Task<CommandResult> StopAsync()
    {
        Log.Information($"Stopping worker {WorkerId}");
        _desiredState = StreamerState.Idle;

        int maxRetries = 3;
        int retryCount = 0;

        // Håndtering af tilstand 'Starting', 'Stopping' eller 'Restarting'
        while ((_streamerRunner.GetState() == StreamerState.Starting ||
                _streamerRunner.GetState() == StreamerState.Stopping ||
                _streamerRunner.GetState() == StreamerState.Restarting) && retryCount < maxRetries)
        {
            Log.Information(
                $"Worker {WorkerId} is in a transitional state ({_streamerRunner.GetState()}). Waiting... (Attempt {retryCount + 1}/{maxRetries})");
            await Task.Delay(500);
            retryCount++;
        }

        if (_streamerRunner.GetState() == StreamerState.Starting ||
            _streamerRunner.GetState() == StreamerState.Stopping ||
            _streamerRunner.GetState() == StreamerState.Restarting)
        {
            Log.Warning(
                $"Worker {WorkerId} is still in a transitional state ({_streamerRunner.GetState()}) after {maxRetries} attempts.");
            return new CommandResult(false,
                $"Worker is still in a transitional state after {maxRetries} attempts. Please try again later.");
        }

        if (_streamerRunner.GetState() == StreamerState.Idle)
        {
            Log.Information($"Worker {WorkerId} is already idle.");
            return new CommandResult(true, "Worker is already idle");
        }

        // Stop watchdog før streameren stoppes
        await _watchdog.StopAsync();
        var (state, message) = await _streamerRunner.StopAsync();

        bool success = state == StreamerState.Idle;

        return new CommandResult(success, message);
    }


    private (bool, string) ShouldRestart()
    {
        // Hvis vi er i gang med en planlagt eller brugerinitieret stopproces, eller hvis vi er i gang med en genstart, skal watchdog ikke genstarte
        if (_desiredState == StreamerState.Idle || _streamerRunner.GetState() == StreamerState.Restarting)
        {
            return (false, "Worker is expected to be idle or is already restarting.");
        }

        // Brug den faktiske state fra _streamerRunner til at afgøre, om en genstart er nødvendig
        var isRunning = _streamerRunner.GetState() == StreamerState.Running;
        var timeSinceLastImage = (DateTime.UtcNow - _lastImageUpdate).TotalSeconds;

        if (!isRunning)
        {
            return (true, "Process not running");
        }

        if (timeSinceLastImage > 2)
        {
            return (true, "Image update missing");
        }

        return (false, string.Empty);
    }

    private async void RestartWorker(string reason)
    {
        string logMessage  = $"Restarting worker {WorkerId} due to: {reason}";
        Log.Information(logMessage );
        Log.Information($"Restarting worker {WorkerId} due to: {reason}");

        // Sæt desired state til Restarting
        _desiredState = StreamerState.Restarting;

        // Stop workeren først
        var stopResult = await StopAsync();
        if (!stopResult.Success)
        {
            Log.Warning($"Failed to stop worker {WorkerId} during restart. Reason: {stopResult.Message}");
            return;
        }

        // Start workeren igen
        var startResult = await StartAsync();
        if (!startResult.Success)
        {
            Log.Warning($"Failed to start worker {WorkerId} during restart. Reason: {startResult.Message}");
            // Hvis vi fejler i at starte, sæt desired state til Idle
            _desiredState = StreamerState.Idle;
        }
    }

    private void OnWatchdogStateChanged(object? sender, string message)
    {
        string logMessage  = $"State changed: {message}";
        Console.WriteLine(logMessage );
        CreateAndSendLog(logMessage );
        
        // for nu fake stop
        //_workerManager.HandleStateChange(WorkerId, StreamerState.Restarting, message);

        // _workerManager.HandleStateChange(WorkerId, _streamerRunner.GetState(), message);

        
        // Håndter stateændringer her, fx log til message-queue eller UI
    }


    private void OnLogGenerated(object? sender, LogEntry log)
    {
        log.LogSequenceNumber = Interlocked.Increment(ref _logCounter);
        _messageQueue.EnqueueMessage(log);
    }

    private void OnImageGenerated(object? sender, ImageData image)
    {
        _messageQueue.EnqueueMessage(image);
        _lastImageUpdate = image.Timestamp;
    }

    public StreamerState GetState()
    {
        return _streamerRunner.GetState();
    }

    private void CreateAndSendLog(string message)
    {
        var log = new LogEntry
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            Message = $"Service: {message}",
            LogSequenceNumber = Interlocked.Increment(ref _logCounter)
        };
        _messageQueue.EnqueueMessage(log);
    }
}