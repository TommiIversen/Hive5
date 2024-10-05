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

        if (worker != null) return await worker.StartAsync();
        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
    }


    public async Task<CommandResult> StopWorkerAsync(Guid workerId)
    {
        Log.Information($"Stopping worker: {workerId}");
        var worker = GetWorker(workerId);

        if (worker != null) return await worker.StopAsync();
        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
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

        // Forsøg at stoppe arbejdstageren, hvis den ikke allerede er 'Idle'
        if (worker.GetState() != StreamerState.Idle)
        {
            Log.Information($"Worker {workerId} is not idle. Attempting to stop before removal...");
            var stopResult = await worker.StopAsync(); // Delegér stop-logikken til `WorkerService`

            if (!stopResult.Success)
            {
                Log.Warning($"Failed to stop worker {workerId} before removal: {stopResult.Message}");
                return new CommandResult(false, $"Failed to stop worker before removal: {stopResult.Message}");
            }
        }

        // Fjern arbejdstageren, når den er stoppet eller allerede er 'Idle'
        SendWorkerEvent(workerId, WorkerEventType.Deleted);

        _workers.Remove(workerId);
        _workersBaseInfo.Remove(workerId);
        Log.Information($"Worker {workerId} successfully removed.");
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
            Log.Warning($"Worker not found for WorkerId: {workerId}");
        }
    }

    // Hjælpefunktion til at få en worker uden brug af `out`
    private WorkerService? GetWorker(Guid workerId)
    {
        return _workers.TryGetValue(workerId, out var worker) ? worker : null;
    }
}