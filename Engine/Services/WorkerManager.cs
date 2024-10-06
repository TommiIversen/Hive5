using Common.Models;
using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Utils;
using Serilog;

namespace Engine.Services;

public class WorkerManager
{
    private readonly MessageQueue _messageQueue;
    private readonly Dictionary<string, WorkerService> _workers = new();

    // gem her til vi får en database
    private readonly Dictionary<string, WorkerOut> _workersBaseInfo = new();
    
    // Repository Factory
    private readonly RepositoryFactory _repositoryFactory;

    public WorkerManager(MessageQueue messageQueue, RepositoryFactory repositoryFactory)
    {
        _messageQueue = messageQueue;
        _repositoryFactory = repositoryFactory;
    }

    public IReadOnlyDictionary<string, WorkerService> Workers => _workers;

    
    public WorkerService AddWorkenew(WorkerCreate workerCreate)
    {
        Log.Information($"Adding worker... {workerCreate.Name}");
        var workerRepository = _repositoryFactory.CreateWorkerRepository();

        // Tjek, om WorkerId allerede findes i databasen
        var existingWorker = workerRepository.GetWorkerByIdAsync(workerCreate.WorkerId).Result;

        // Hvis arbejderen allerede findes i databasen
        if (existingWorker != null)
        {
            Log.Warning($"Worker with ID {workerCreate.WorkerId} already exists in database.");
            // Tjek om arbejderen også findes i _workers, ellers opret ny WorkerService for den
            return GetOrCreateWorkerService(workerCreate, new FakeStreamerRunner());
        }

        // Hvis arbejderen ikke findes, opret en ny i databasen og i _workers
        IStreamerRunner streamerRunner = new FakeStreamerRunner();
        var workerId = workerCreate.WorkerId ?? Guid.NewGuid().ToString();
        workerCreate.WorkerId = workerId;

        var worker = GetOrCreateWorkerService(workerCreate, streamerRunner);

        Console.WriteLine($"Adding worker to database.. {workerCreate.WorkerId}");

        var workerEntity = new WorkerEntity
        {
            WorkerId = workerCreate.WorkerId,
            Name = workerCreate.Name,
            Description = workerCreate.Description,
            Command = workerCreate.Command,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        workerRepository.AddWorkerAsync(workerEntity).Wait(); // Synchronous call for simplicity, async is preferred

        return worker;
    }
    
    private WorkerService GetOrCreateWorkerService(WorkerCreate workerCreate, IStreamerRunner streamerRunner)
    {
        // Hvis arbejderen allerede findes i databasen og i _workers, returner den eksisterende service
        if (_workers.ContainsKey(workerCreate.WorkerId))
        {
            Log.Information($"Worker with ID {workerCreate.WorkerId} already exists in memory. Returning existing service.");
            return _workers[workerCreate.WorkerId];
        }

        // Ellers opret en ny service for arbejderen
        var workerService = new WorkerService(_messageQueue, streamerRunner, workerCreate.WorkerId);
        _workers[workerCreate.WorkerId] = workerService;

        return workerService;
    }
    
    public WorkerService AddWorker(WorkerCreate workerCreate)
    {
        Log.Information($"Adding worker... {workerCreate.Name}");
        IStreamerRunner streamerRunner = new FakeStreamerRunner();

        var worker = new WorkerService(_messageQueue, streamerRunner, workerCreate.WorkerId);

        var workerOut = new WorkerOut
        {
            WorkerId = workerCreate.WorkerId,
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
    

    public async Task<CommandResult> StartWorkerAsync(string workerId)
    {
        Log.Information($"Starting worker: {workerId}");
        var worker = GetWorker(workerId);

        if (worker != null) return await worker.StartAsync();
        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
    }


    public async Task<CommandResult> StopWorkerAsync(string workerId)
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

    public async Task<CommandResult> RemoveWorkerAsync(string workerId)
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

    private void SendWorkerEvent(string workerId, WorkerEventType eventType)
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
    private WorkerService? GetWorker(string workerId)
    {
        return _workers.TryGetValue(workerId, out var worker) ? worker : null;
    }
}