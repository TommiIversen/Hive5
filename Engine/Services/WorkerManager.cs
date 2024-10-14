using Common.Models;
using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Models;
using Engine.Utils;
using Serilog;

namespace Engine.Services;

public class WorkerManager(MessageQueue messageQueue, RepositoryFactory repositoryFactory)
{
    private readonly Dictionary<string, WorkerService> _workers = new();
    public IReadOnlyDictionary<string, WorkerService> Workers => _workers;

    
    public async Task InitializeWorkersAsync()
    {
        Log.Information("Initializing workers from database...");
    
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var allWorkers = await workerRepository.GetAllWorkersAsync();

        // Filtrer workers, der er IsEnabled
        var enabledWorkers = allWorkers.ToList();

        foreach (var workerEntity in enabledWorkers)
        {
            // Tjek om arbejderen allerede findes i in-memory _workers
            if (_workers.ContainsKey(workerEntity.WorkerId))
            {
                Log.Information($"Worker with ID {workerEntity.WorkerId} already exists in memory.");
                continue;
            }

            // Hvis arbejderen ikke findes i _workers, opret en ny service for arbejderen
            IStreamerRunner streamerRunner = new FakeStreamerRunner();
            var workerService = new WorkerService(this, messageQueue, streamerRunner, workerEntity.WorkerId);
            _workers[workerEntity.WorkerId] = workerService;

            // Start arbejderen if enabled
            if (workerEntity.IsEnabled)
            {
                await StartWorkerAsync(workerService.WorkerId);
            }
        }

        Log.Information("Workers initialized successfully.");
    }



    public WorkerService AddWorker(WorkerCreate workerCreate)
    {
        Log.Information($"Adding worker... {workerCreate.Name}");
        IWorkerRepository workerRepository = repositoryFactory.CreateWorkerRepository();

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
        var workerService = new WorkerService(this, messageQueue, streamerRunner, workerCreate.WorkerId);
        _workers[workerCreate.WorkerId] = workerService;

        return workerService;
    }


    public async Task<CommandResult> StartWorkerAsync(string workerId)
    {
        Log.Information($"Starting worker: {workerId}");
        var worker = GetWorkerService(workerId);

        if (worker != null)
        {
            var result = await worker.StartAsync();
            //await SendWorkerEvent(workerId, WorkerEventType.Updated);
            return result;
        }

        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
    }


    public async Task<CommandResult> StopWorkerAsync(string workerId)
    {
        Log.Information($"Stopping worker: {workerId}");
        var worker = GetWorkerService(workerId);

        if (worker != null)
        {
            var result = await worker.StopAsync();
            //await SendWorkerEvent(workerId, WorkerEventType.Updated);
            return result;
        }

        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
    }


    public async Task<List<WorkerEvent>> GetAllWorkers(Guid engineId)
    {
        Log.Information($"Getting all workers from database...");

        var workerRepository = repositoryFactory.CreateWorkerRepository();
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

    private WorkerState GetWorkerState(string workerId)
    {
        var workerService = GetWorkerService(workerId);
        if (workerService != null)
        {
            return workerService.GetState();
        }

        return WorkerState.Idle;
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
        if (worker.GetState() != WorkerState.Idle)
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
        var workerRepository = repositoryFactory.CreateWorkerRepository();
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
            Name = "WorkerDeleted",
            Description = "Worker has been deleted",
            Command = "WorkerDeleted",
            WorkerId = workerId,
            EventType = WorkerEventType.Deleted,
            Timestamp = DateTime.UtcNow,
            IsEnabled = false,
        };
        messageQueue.EnqueueMessage(workerEvent);
    }


    private async Task SendWorkerEvent(string workerId, WorkerEventType eventType)
    {
        Console.WriteLine($"SendWorkerEvent: Sending worker event: {eventType} - {workerId}");
        var workerService = GetWorkerService(workerId);


        if (workerService != null)
        {
            var workerRepository = repositoryFactory.CreateWorkerRepository();
            var workerEntity = await workerRepository.GetWorkerByIdAsync(workerId);
            if (workerEntity != null)
            {
                var workerEvent = workerEntity.ToWorkerEvent(workerService.GetState(), eventType);
                messageQueue.EnqueueMessage(workerEvent);
            }
        }
        else
        {
            Log.Warning($"SendWorkerEvent: Worker not found for WorkerId: {workerId}");
        }
    }

    
    
    public async Task HandleStateChange(WorkerService workerService, WorkerState newState, WorkerEventType eventType = WorkerEventType.Updated, string reason = "")
    {
        var logMessage = $"Worker {workerService.WorkerId} state changed to {newState}: {reason}";
        Log.Information(logMessage);

        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerService.WorkerId);

        if (workerEntity != null)
        {
            var workerEvent = workerEntity.ToWorkerEvent(newState, eventType);
            //workerEvent.Reason = reason; // Tilføj årsag hvis relevant
            messageQueue.EnqueueMessage(workerEvent);
        }
        else
        {
            Log.Warning($"Worker with ID {workerService.WorkerId} not found in database.");
        }
    }
    
    
    private WorkerService? GetWorkerService(string workerId)
    {
        return _workers.TryGetValue(workerId, out var worker) ? worker : null;
    }
}