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
        var enabledWorkers = allWorkers.ToList();

        foreach (var workerEntity in enabledWorkers)
        {
            // Tjek om arbejderen allerede findes i in-memory _workers
            if (_workers.ContainsKey(workerEntity.WorkerId))
            {
                Log.Information($"Worker with ID {workerEntity.WorkerId} already exists in memory.");
                continue;
            }

            // Hvis workeren ikke findes i _workers, opret en ny service for arbejderen
            IStreamerRunner streamerRunner = new FakeStreamerRunner
            {
                WorkerId = workerEntity.WorkerId
            };
            var workerService = new WorkerService(this, messageQueue, streamerRunner, workerEntity.WorkerId,
                repositoryFactory);
            _workers[workerEntity.WorkerId] = workerService;

            // Start arbejderen if enabled
            if (workerEntity.IsEnabled)
            {
                await StartWorkerAsync(workerService.WorkerId);
            }
        }

        Log.Information("Workers initialized successfully.");
    }

    public async Task<WorkerService?> AddWorkerAsync(Guid engineId, WorkerCreate workerCreate)
    {
        Log.Information($"Adding worker... {workerCreate.Name}");
        IWorkerRepository workerRepository = repositoryFactory.CreateWorkerRepository();

        // Tjek, om WorkerId allerede findes i databasen asynkront
        var existingWorker = await workerRepository.GetWorkerByIdAsync(workerCreate.WorkerId);

        // Hvis arbejderen allerede findes i databasen, returnér null
        if (existingWorker != null)
        {
            Log.Warning($"Worker with ID {workerCreate.WorkerId} already exists in database.");
            return null;
        }

        // Hvis WorkerId ikke er angivet, generer et random ID
        if (string.IsNullOrWhiteSpace(workerCreate.WorkerId))
        {
            workerCreate.WorkerId = Guid.NewGuid().ToString();
        }

        IStreamerRunner streamerRunner = new FakeStreamerRunner
        {
            WorkerId = workerCreate.WorkerId
        };
        var workerService = GetOrCreateWorkerService(workerCreate, streamerRunner);

        Log.Information($"Adding worker to database.. {workerCreate.WorkerId}");

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

        // Tilføj arbejderen til databasen asynkront
        await workerRepository.AddWorkerAsync(workerEntity);
        await SendWorkerEvent(workerCreate.WorkerId, EventType.Created);
        return workerService;
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
        var workerService =
            new WorkerService(this, messageQueue, streamerRunner, workerCreate.WorkerId, repositoryFactory);
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
            return result;
        }

        Log.Warning($"Worker with ID {workerId} not found.");
        return new CommandResult(false, "Worker not found");
    }

    public async Task<CommandResult> EnableDisableWorkerAsync(string workerId, bool enable)
    {
        Log.Information($"{(enable ? "Enabling" : "Disabling")} worker: {workerId}");

        // Tjek om arbejderen eksisterer
        var worker = GetWorkerService(workerId);
        if (worker == null)
        {
            Log.Warning($"Worker with ID {workerId} not found.");
            return new CommandResult(false, "Worker not found");
        }

        // Hvis vi skal disable arbejderen, stop den først, hvis den er i gang
        if (!enable)
        {
            if (worker.GetState() != WorkerState.Idle)
            {
                Log.Information($"Stopping worker {workerId} before disabling...");
                var stopResult = await worker.StopAsync();
                if (!stopResult.Success)
                {
                    Log.Warning($"Failed to stop worker {workerId}: {stopResult.Message}");
                    return new CommandResult(false, $"Failed to stop worker: {stopResult.Message}");
                }
            }
        }

        // Opdater databasen for at sætte IsEnabled
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerId);
        if (workerEntity == null)
        {
            Log.Warning($"Worker entity with ID {workerId} not found in database.");
            return new CommandResult(false, "Worker not found in database");
        }

        workerEntity.IsEnabled = enable;
        await workerRepository.UpdateWorkerAsync(workerEntity);

        // Hvis vi skal enable arbejderen, start den
        if (enable)
        {
            var startResult = await worker.StartAsync();
            if (!startResult.Success)
            {
                Log.Warning($"Failed to start worker {workerId} after enabling: {startResult.Message}");
                return new CommandResult(false, $"Worker enabled, but failed to start: {startResult.Message}");
            }
        }

        // Send event om at arbejderen er blevet enabled/disabled
        await SendWorkerEvent(workerId, EventType.Updated);

        Log.Information($"Worker {workerId} has been successfully {(enable ? "enabled" : "disabled")}.");
        return new CommandResult(true, $"Worker {workerId} {(enable ? "enabled" : "disabled")} successfully");
    }

    public async Task<CommandResult> EditWorkerAsync(string workerId, string newName, string newDescription,
        string? newCommand)
    {
        Log.Information($"Editing worker: {workerId}");

        // Hent arbejderen fra databasen
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerId);

        if (workerEntity == null)
        {
            Log.Warning($"Worker with ID {workerId} not found in database.");
            return new CommandResult(false, "Worker not found in database");
        }

        // Tjek om der er ændringer i Name, Description eller Command
        string isModified = "";

        if (workerEntity.Name != newName)
        {
            workerEntity.Name = newName;
            isModified = "New Name";
        }

        if (workerEntity.Description != newDescription)
        {
            workerEntity.Description = newDescription;
            isModified = "New Description";
        }

        var commandChanged = workerEntity.Command != newCommand;
        if (commandChanged)
        {
            workerEntity.Command = newCommand;
            isModified = "New Command";
        }

        if (string.IsNullOrEmpty(isModified))
        {
            Log.Information($"No changes detected for worker {workerId}.");
            return new CommandResult(true, "No changes detected");
        }

        workerEntity.UpdatedAt = DateTime.UtcNow;

        await workerRepository.UpdateWorkerAsync(workerEntity);

        // Hvis kommandoen er ændret, genstart arbejderen med den nye kommando
        if (commandChanged)
        {
            var workerService = GetWorkerService(workerId);
            if (workerService != null)
            {
                Log.Information($"Restarting worker {workerId} due to command change.");
                await workerService.StopAsync(); // Stop arbejderen
                var result = await workerService.StartAsync(); // Genstart med den nye kommando
                if (!result.Success)
                {
                    Log.Warning($"Failed to restart worker {workerId}: {result.Message}");
                    return new CommandResult(false, $"Worker updated but failed to restart: {result.Message}");
                }
            }
        }

        // Send en event om at arbejderen er blevet opdateret
        await SendWorkerEvent(workerId, EventType.Updated);

        Log.Information($"Worker {workerId} updated successfully.");
        return new CommandResult(true, $"Worker updated successfully: {isModified}");
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

    public async Task<CommandResult> ResetWatchdogEventCountAsync(string workerId)
    {
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerId);

        if (workerEntity != null)
        {
            // Nulstil tælleren
            workerEntity.WatchdogEventCount = 0;
            await workerRepository.UpdateWorkerAsync(workerEntity);
            Log.Information($"Watchdog event count reset for worker {workerId}.");
            await SendWorkerEvent(workerId, EventType.Updated);
            return new CommandResult(true, "Watchdog event count reset successfully.");
        }
        else
        {
            Log.Warning($"Worker with ID {workerId} not found.");
            return new CommandResult(false, "Worker not found.");
        }
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
            EventType = EventType.Deleted,
            Timestamp = DateTime.UtcNow,
            IsEnabled = false,
            WatchdogEventCount = 0
        };
        messageQueue.EnqueueMessage(workerEvent);
    }

    private async Task SendWorkerEvent(string workerId, EventType eventType)
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

    public async Task HandleStateChange(WorkerService workerService, WorkerState newState,
        EventType eventType = EventType.Updated, string reason = "")
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