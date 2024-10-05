using Common.Models;
using Engine.Interfaces;
using Engine.Utils;
using Serilog;

namespace Engine.Services;

public class WorkerManager
{
    private readonly MessageQueue _messageQueue;
    private readonly Dictionary<Guid, WorkerService> _workers = new();

    // gem her til vi får en database
    private readonly Dictionary<Guid, WorkerOut> _workersBaseInfo = new();

    public WorkerManager(MessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
    }

    public IReadOnlyDictionary<Guid, WorkerService> Workers => _workers;

    public WorkerService AddWorker(WorkerCreate workerCreate)
    {
        Log.Information($"Adding worker... {workerCreate.Name}");
        IStreamerRunner streamerRunner = new FakeStreamerRunner();

        var worker = new WorkerService(_messageQueue, streamerRunner);

        var workerOut = new WorkerOut
        {
            WorkerId = worker.WorkerId,
            Name = workerCreate.Name,
            Description = workerCreate.Description,
            Command = workerCreate.Command,
            Enabled = true,
            State = StreamerState.Idle
        };
        _workers[worker.WorkerId] = worker;
        _workersBaseInfo[worker.WorkerId] = workerOut;
        return worker;
    }

    public async Task<CommandResult> StartWorkerAsync(Guid workerId)
    {
        Log.Information($"Starting worker: {workerId}");
        var worker = GetWorker(workerId);

        if (worker == null)
        {
            Log.Warning($"Worker with ID {workerId} not found.");
            return new CommandResult(false, "Worker not found");
        }

        // Hvis worker er i 'Stopping' eller 'Starting' tilstand, vent indtil den er 'Idle' eller 'Running'
        while (worker.GetState() == StreamerState.Stopping || worker.GetState() == StreamerState.Starting)
        {
            if (worker.GetState() == StreamerState.Stopping)
            {
                Log.Information(
                    $"Worker {workerId} is currently stopping. Waiting for it to complete before starting...");
            }
            else if (worker.GetState() == StreamerState.Starting)
            {
                Log.Information($"Worker {workerId} is currently starting. Waiting for it to complete...");
            }

            await Task.Delay(500); // Vent i 500ms før vi tjekker igen
        }

        // Hvis worker er i 'Running' tilstand, returner, at arbejderen allerede kører
        if (worker.GetState() == StreamerState.Running)
        {
            Log.Information($"Worker {workerId} is already running.");
            return new CommandResult(true, "Worker is already running");
        }

        // Hvis worker er i 'Idle' tilstand, forsøg at starte den
        if (worker.GetState() == StreamerState.Idle)
        {
            var startResult = await worker.StartAsync();
            if (startResult.Success)
            {
                SendWorkerEvent(workerId, WorkerEventType.Updated);
                return new CommandResult(true, "Worker started successfully");
            }
            else
            {
                Log.Warning($"Failed to start worker {workerId}: {startResult.Message}");
                return new CommandResult(false, $"Failed to start worker: {startResult.Message}");
            }
        }

        // Hvis vi ikke kan identificere tilstanden, returnerer vi en fejl
        Log.Warning($"Worker {workerId} is in an unexpected state: {worker.GetState()}.");
        return new CommandResult(false, $"Unexpected state for worker {workerId}");
    }


    public async Task<CommandResult> StopWorkerAsync(Guid workerId)
    {
        Log.Information($"Stopping worker: {workerId}");
        var worker = GetWorker(workerId);
        if (worker == null)
        {
            Log.Warning($"Worker with ID {workerId} not found.");
            return new CommandResult(false, "Worker not found");
        }

        // Hvis worker er i 'Starting' eller 'Stopping' tilstand, vent indtil den er 'Idle' eller 'Running'
        while (worker.GetState() == StreamerState.Starting || worker.GetState() == StreamerState.Stopping)
        {
            Log.Information(
                $"Worker {workerId} is currently in state {worker.GetState()}. Waiting for it to complete...");
            await Task.Delay(500); // Vent i 500ms før vi tjekker igen
        }

        // Hvis worker er i 'Running' tilstand, forsøg at stoppe den
        if (worker.GetState() == StreamerState.Running)
        {
            var stopResult = await worker.StopAsync();
            if (!stopResult.Success)
            {
                Log.Warning($"Failed to stop worker {workerId}: {stopResult.Message}");
                return new CommandResult(false, $"Failed to stop worker: {stopResult.Message}");
            }

            SendWorkerEvent(workerId, WorkerEventType.Updated);
            return new CommandResult(true, "Worker stopped successfully");
        }

        // Hvis worker nu er i 'Idle' tilstand, kan vi returnere succes
        if (worker.GetState() == StreamerState.Idle)
        {
            Log.Information($"Worker {workerId} is already idle.");
            return new CommandResult(true, "Worker is already idle");
        }

        // Hvis vi ikke kan identificere tilstanden, returnerer vi en fejl
        Log.Warning($"Worker {workerId} is in an unexpected state: {worker.GetState()}.");
        return new CommandResult(false, $"Unexpected state for worker {workerId}");
    }


    public List<WorkerOut> GetAllWorkers(Guid engineId)
    {
        // FIX - inject ikke enginID her
        Log.Information($"Getting all workers...");

        foreach (var worker in _workersBaseInfo.Values)
            worker.EngineId = engineId;

        return _workersBaseInfo.Values.ToList();
    }

    public async Task<CommandResult> RemoveWorkerAsync(Guid workerId)
    {
        Log.Information($"Removing worker: {workerId}");
        var worker = GetWorker(workerId);

        if (worker == null)
        {
            Log.Warning($"Worker with ID {workerId} not found.");
            return new CommandResult(false, "Worker not found");
        }

        // Forsøg at stoppe worker, hvis den ikke allerede er i 'Idle'
        if (worker.GetState() != StreamerState.Idle)
        {
            Log.Information($"Worker {workerId} is not idle. Attempting to stop before removal...");
            var stopResult = await StopWorkerAsync(workerId);
            if (!stopResult.Success)
            {
                return new CommandResult(false, $"Failed to stop worker before removal: {stopResult.Message}");
            }
        }

        // Fjern worker efter den er stoppet eller allerede er 'Idle'
        _workers.Remove(workerId);
        _workersBaseInfo.Remove(workerId);
        Log.Information($"Worker {workerId} successfully removed.");

        SendWorkerEvent(workerId, WorkerEventType.Deleted);

        return new CommandResult(true, "Worker removed successfully");
    }

    private void SendWorkerEvent(Guid workerId, WorkerEventType eventType)
    {
        if (_workersBaseInfo.TryGetValue(workerId, out var workerOut))
        {
            // Opdater `IsRunning` status baseret på `WorkerService`
            var workerService = GetWorker(workerId);
            if (workerService != null)
            {
                workerOut.State = workerService.GetState();
            }

            var workerEvent = workerOut.ToWorkerEvent(eventType);
            _messageQueue.EnqueueMessage(workerEvent);
        }
        else
        {
            Log.Warning($"WorkerOut not found for WorkerId: {workerId}");
        }
    }

    // Hjælpefunktion til at få en worker uden brug af `out`
    private WorkerService? GetWorker(Guid workerId)
    {
        return _workers.TryGetValue(workerId, out var worker) ? worker : null;
    }
}