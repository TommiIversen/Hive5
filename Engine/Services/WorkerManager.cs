using Common.Models;
using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Models;
using Engine.Utils;
using Serilog;

namespace Engine.Services;

public class WorkerManager
{
    private readonly MessageQueue _messageQueue;
    private readonly Dictionary<string, WorkerService> _workers = new();
    private readonly RepositoryFactory _repositoryFactory;
    public IReadOnlyDictionary<string, WorkerService> Workers => _workers;

    public WorkerManager(MessageQueue messageQueue, RepositoryFactory repositoryFactory)
    {
        _messageQueue = messageQueue;
        _repositoryFactory = repositoryFactory;
    }


    public WorkerService AddWorker(WorkerCreate workerCreate)
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
            Log.Information(
                $"Worker with ID {workerCreate.WorkerId} already exists in memory. Returning existing service.");
            return _workers[workerCreate.WorkerId];
        }

        // Ellers opret en ny service for arbejderen
        var workerService = new WorkerService(_messageQueue, streamerRunner, workerCreate.WorkerId);
        _workers[workerCreate.WorkerId] = workerService;

        return workerService;
    }


    public async Task<CommandResult> StartWorkerAsync(string workerId)
    {
        Log.Information($"Starting worker: {workerId}");
        var worker = GetWorkerService(workerId);

        if (worker != null) return await worker.StartAsync();
        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
    }


    public async Task<CommandResult> StopWorkerAsync(string workerId)
    {
        Log.Information($"Stopping worker: {workerId}");
        var worker = GetWorkerService(workerId);

        if (worker != null) return await worker.StopAsync();
        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
    }


    public async Task<List<WorkerEvent>> GetAllWorkers(Guid engineId)
    {
        Log.Information($"Getting all workers from database...");

        var workerRepository = _repositoryFactory.CreateWorkerRepository();
        var workerEntities = await workerRepository.GetAllWorkersAsync();

        // Map WorkerEntity til WorkerEvent direkte med opdateret state fra WorkerService
        var workerEvents = workerEntities
            .Select(workerEntity =>
            {
                var state = GetWorkerState(workerEntity.WorkerId); // Hent den aktuelle state
                return workerEntity.ToWorkerEvent(engineId, state);
            })
            .ToList();

        return workerEvents;
    }

    private StreamerState GetWorkerState(string workerId)
    {
        var workerService = GetWorkerService(workerId);
        if (workerService != null)
        {
            return workerService.GetState();
        }

        return StreamerState.Idle;
    }

    public async Task<CommandResult> RemoveWorkerAsync(string workerId)
    {
        Log.Information($"Removing worker: {workerId}");
        var worker = GetWorkerService(workerId);

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

        // Fjern woker fra databasen først
        var workerRepository = _repositoryFactory.CreateWorkerRepository();
        await workerRepository.DeleteWorkerAsync(workerId);


        _workers.Remove(workerId);
        Log.Information($"Worker {workerId} successfully removed.");

        SendWorkerDeletedEvent(workerId);


        return new CommandResult(true, "Worker removed successfully");
    }

    private void SendWorkerDeletedEvent(string workerId)
    {
        Log.Information($"Sending delete event for worker: {workerId}");
        var workerEvent = new WorkerEvent
        {
            WorkerId = workerId,
            EventType = WorkerEventType.Deleted,
            Timestamp = DateTime.UtcNow
        };
        _messageQueue.EnqueueMessage(workerEvent);
    }

    private async Task SendWorkerEvent(string workerId, WorkerEventType eventType)
    {
        Console.WriteLine($"SendWorkerEvent: Sending worker event: {eventType} - {workerId}");
        var workerService = GetWorkerService(workerId);


        if (workerService != null)
        {
            var workerRepository = _repositoryFactory.CreateWorkerRepository();
            var workerEntity = await workerRepository.GetWorkerByIdAsync(workerId);
            if (workerEntity != null)
            {
                var workerEvent = workerEntity.ToWorkerEvent(workerService.GetState(), eventType);
                _messageQueue.EnqueueMessage(workerEvent);
            }
        }
        else
        {
            Log.Warning($"SendWorkerEvent: Worker not found for WorkerId: {workerId}");
        }
    }

    private WorkerService? GetWorkerService(string workerId)
    {
        return _workers.TryGetValue(workerId, out var worker) ? worker : null;
    }
}