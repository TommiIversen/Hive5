using Common.DTOs;
using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Models;
using Engine.Utils;
using Serilog;

namespace Engine.Services;

public interface IWorkerManager
{
    IReadOnlyDictionary<string, IWorkerService> Workers { get; }
    Task InitializeWorkersAsync();
    Task<IWorkerService?> AddWorkerAsync(Guid engineId, WorkerCreateAndEdit workerCreateAndEdit);
    Task<CommandResult> StartWorkerAsync(string workerId);
    Task<CommandResult> StopWorkerAsync(string workerId);
    Task<CommandResult> EnableDisableWorkerAsync(string workerId, bool enable);

    Task<CommandResult> EditWorkerAsync(WorkerCreateAndEdit workerEdit);

    Task<List<WorkerChangeEvent>> GetAllWorkers(Guid engineId);
    Task<CommandResult> RemoveWorkerAsync(string workerId);
    Task<CommandResult> ResetWatchdogEventCountAsync(string workerId);
    Task<WorkerEventWithLogsDto> GetWorkerEventsWithLogsAsync(string workerId);
}

public class WorkerManager(
    IMessageQueue messageQueue,
    RepositoryFactory repositoryFactory,
    ILoggerService loggerService,
    StreamerWatchdogFactory watchdogFactory)
    : IWorkerManager
{
    private readonly Dictionary<string, IWorkerService> _workers = new();
    public IReadOnlyDictionary<string, IWorkerService> Workers => _workers;

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
                LogInfo($"Worker with ID {workerEntity.WorkerId} already exists in memory.", workerEntity.WorkerId,
                    LogLevel.Warning);

                continue;
            }

            IStreamerService streamerService = new FakeStreamerService
            {
                WorkerId = workerEntity.WorkerId,
                GstCommand = workerEntity.Command
            };

            // Opret WorkerConfiguration fra WorkerEntity med FromEntity metoden
            var workerConfig = WorkerConfiguration.FromEntity(workerEntity);
            var workerService = new WorkerService(
                loggerService,
                messageQueue,
                streamerService,
                repositoryFactory,
                watchdogFactory,
                workerConfig);
            _workers[workerEntity.WorkerId] = workerService;

            // Start arbejderen if enabled
            if (workerEntity.IsEnabled) await StartWorkerAsync(workerEntity.WorkerId);
        }

        Log.Information("Workers initialized successfully.");
    }

    public async Task<IWorkerService?> AddWorkerAsync(Guid engineId, WorkerCreateAndEdit workerCreateAndEdit)
    {
        LogInfo($"Adding worker... {workerCreateAndEdit.Name}", workerCreateAndEdit.WorkerId);

        var workerRepository = repositoryFactory.CreateWorkerRepository();

        // Tjek, om WorkerId allerede findes i databasen asynkront
        var existingWorker = await workerRepository.GetWorkerByIdAsync(workerCreateAndEdit.WorkerId);

        // Hvis arbejderen allerede findes i databasen, returnér null
        if (existingWorker != null)
        {
            LogInfo($"Worker with ID {workerCreateAndEdit.WorkerId} already exists in database.",
                workerCreateAndEdit.WorkerId,
                LogLevel.Warning);
            return null;
        }

        // Hvis WorkerId ikke er angivet, generer et random ID
        if (string.IsNullOrWhiteSpace(workerCreateAndEdit.WorkerId))
            workerCreateAndEdit.WorkerId = Guid.NewGuid().ToString();

        // Hvis arbejderen allerede findes i databasen og i _workers, returner den eksisterende service
        if (_workers.TryGetValue(workerCreateAndEdit.WorkerId, out var async))
        {
            var logmessage =
                $"Worker with ID {workerCreateAndEdit.WorkerId} already exists in memory. Returning existing service.";
            LogInfo(logmessage, workerCreateAndEdit.WorkerId, LogLevel.Warning);
            return async;
        }


        LogInfo($"Adding worker to database.. {workerCreateAndEdit.WorkerId}", workerCreateAndEdit.WorkerId);
        var workerEntity = new WorkerEntity
        {
            WorkerId = workerCreateAndEdit.WorkerId,
            Name = workerCreateAndEdit.Name,
            Description = workerCreateAndEdit.Description,
            Command = workerCreateAndEdit.Command,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ImgWatchdogEnabled = workerCreateAndEdit.ImgWatchdogEnabled,
        };

        // Tilføj worker til databasen asynkront
        await workerRepository.AddWorkerAsync(workerEntity);
        
        IStreamerService streamerService = new FakeStreamerService
        {
            WorkerId = workerCreateAndEdit.WorkerId,
            GstCommand = workerCreateAndEdit.Command
        };

        // opret en ny service for workeren
        var workerConfig = WorkerConfiguration.FromEntity(workerEntity);
        var workerService = new WorkerService(
            loggerService,
            messageQueue,
            streamerService,
            repositoryFactory,
            watchdogFactory,
            workerConfig);
        _workers[workerCreateAndEdit.WorkerId] = workerService;

        await SendWorkerEvent(workerCreateAndEdit.WorkerId, EventType.Created);
        return workerService;
    }

    public async Task<CommandResult> StartWorkerAsync(string workerId)
    {
        LogInfo($"Starting worker: {workerId}", workerId);

        var worker = GetWorkerService(workerId);

        if (worker != null)
        {
            var result = await worker.StartAsync();
            return result;
        }

        LogInfo($"Worker with ID {workerId} not found.", workerId, LogLevel.Warning);
        return new CommandResult(false, "Worker not found");
    }

    public async Task<CommandResult> StopWorkerAsync(string workerId)
    {
        LogInfo($"Stopping worker: {workerId}", workerId);
        var worker = GetWorkerService(workerId);

        if (worker != null)
        {
            var result = await worker.StopAsync();
            return result;
        }

        LogInfo($"Worker with ID {workerId} not found.", workerId, LogLevel.Warning);
        return new CommandResult(false, "Worker not found");
    }

    public async Task<CommandResult> EnableDisableWorkerAsync(string workerId, bool enable)
    {
        LogInfo($"{(enable ? "Enabling" : "Disabling")} worker: {workerId}", workerId);

        // Tjek om arbejderen eksisterer
        var worker = GetWorkerService(workerId);
        if (worker == null)
        {
            Log.Warning($"Worker with ID {workerId} not found.");
            return new CommandResult(false, "Worker not found");
        }

        // Hvis vi skal disable arbejderen, stop den først, hvis den er i gang
        if (!enable)
            if (worker.GetState() != WorkerState.Idle)
            {
                LogInfo($"Stopping worker {workerId} before disabling...", workerId);
                var stopResult = await worker.StopAsync();
                if (!stopResult.Success)
                {
                    LogInfo($"Failed to stop worker {workerId}: {stopResult.Message}", workerId, LogLevel.Error);
                    return new CommandResult(false, $"Failed to stop worker: {stopResult.Message}");
                }
            }

        // Opdater databasen for at sætte IsEnabled
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerId);
        if (workerEntity == null)
        {
            LogInfo($"Worker entity with ID {workerId} not found in database.", workerId, LogLevel.Warning);
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
                LogInfo($"Failed to start worker {workerId} after enabling: {startResult.Message}", workerId,
                    LogLevel.Error);
                return new CommandResult(false, $"Worker enabled, but failed to start: {startResult.Message}");
            }
        }

        // Send event om at arbejderen er blevet enabled/disabled
        await SendWorkerEvent(workerId, EventType.Updated);

        LogInfo($"Worker {workerId} has been successfully {(enable ? "enabled" : "disabled")}.", workerId);
        return new CommandResult(true, $"Worker {workerId} {(enable ? "enabled" : "disabled")} successfully");
    }

    public async Task<CommandResult> EditWorkerAsync(WorkerCreateAndEdit workerEdit)
    {
        LogInfo($"Editing worker: {workerEdit.WorkerId}", workerEdit.WorkerId);

        // Hent arbejderen fra databasen
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerEdit.WorkerId);

        if (workerEntity == null)
        {
            LogInfo($"Worker with ID {workerEdit.WorkerId} not found in database.", workerEdit.WorkerId,
                LogLevel.Warning);
            return new CommandResult(false, "Worker not found in database");
        }

        // Tjek om der er ændringer i Name, Description eller Command
        var isModified = "";
        var commandChanged = false;


        if (workerEntity.Name != workerEdit.Name)
        {
            workerEntity.Name = workerEdit.Name;
            isModified = "New Name";
        }

        if (workerEntity.Description != workerEdit.Description)
        {
            workerEntity.Description = workerEdit.Description;
            isModified = "New Description";
        }

        commandChanged = workerEntity.Command != workerEdit.Command;
        if (commandChanged)
        {
            workerEntity.Command = workerEdit.Command ?? string.Empty;
            isModified = "New Command";
        }


        // Tjek og opdater Watchdog felter hvis ændret
        var watchdogChanged = false;

        if (workerEntity.ImgWatchdogEnabled != workerEdit.ImgWatchdogEnabled ||
            workerEntity.ImgWatchdogInterval != workerEdit.ImgWatchdogInterval ||
            workerEntity.ImgWatchdogGraceTime != workerEdit.ImgWatchdogGraceTime)
        {
            watchdogChanged = true;
            workerEntity.ImgWatchdogEnabled = workerEdit.ImgWatchdogEnabled;
            workerEntity.ImgWatchdogInterval = workerEdit.ImgWatchdogInterval;
            workerEntity.ImgWatchdogGraceTime = workerEdit.ImgWatchdogGraceTime;
            isModified = "Watchdog Settings";
        }

        if (string.IsNullOrEmpty(isModified) && !watchdogChanged)
        {
            LogInfo("No changes detected", workerEdit.WorkerId);
            return new CommandResult(true, "No changes detected");
        }

        workerEntity.UpdatedAt = DateTime.UtcNow;

        await workerRepository.UpdateWorkerAsync(workerEntity);


        if (watchdogChanged)
        {
            var workerService = GetWorkerService(workerEdit.WorkerId);
            workerService?.UpdateWatchdogSettingsAsync(workerEdit.ImgWatchdogEnabled,
                workerEdit.ImgWatchdogInterval, workerEdit.ImgWatchdogGraceTime);
        }

        // Hvis kommandoen er ændret, genstart arbejderen med den nye kommando
        if (commandChanged)
        {
            var workerService = GetWorkerService(workerEdit.WorkerId);
            if (workerService != null)
            {
                LogInfo($"Restarting worker {workerEdit.WorkerId} due to command change.", workerEdit.WorkerId);
                await workerService.StopAsync(); // Stop arbejderen
                workerService.SetGstCommand(workerEdit.Command ?? string.Empty); // Opdater kommandoen
                var result = await workerService.StartAsync(); // Genstart med den nye kommando
                if (!result.Success)
                {
                    LogInfo($"Failed to restart worker {workerEdit.WorkerId}: {result.Message}", workerEdit.WorkerId,
                        LogLevel.Error);
                    return new CommandResult(false, $"Worker updated but failed to restart: {result.Message}");
                }
            }
        }

        // Send en event om at arbejderen er blevet opdateret
        await SendWorkerEvent(workerEdit.WorkerId, EventType.Updated);

        LogInfo($"Worker updated successfully: {isModified}", workerEdit.WorkerId);
        return new CommandResult(true, $"Worker updated successfully: {isModified}");
    }


    public async Task<List<WorkerChangeEvent>> GetAllWorkers(Guid engineId)
    {
        Log.Information("Getting all workers from database...");

        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntities = await workerRepository.GetAllWorkersAsync();

        // Map WorkerEntity til WorkerChangeEvent direkte med opdateret state fra WorkerService
        var workerEvents = workerEntities
            .Select(workerEntity =>
            {
                var state = GetWorkerState(workerEntity.WorkerId); // Hent den aktuelle state
                return workerEntity.ToWorkerEvent(engineId, state);
            })
            .ToList();

        return workerEvents;
    }

    public async Task<CommandResult> RemoveWorkerAsync(string workerId)
    {
        LogInfo($"Removing worker: {workerId}", workerId);
        var worker = GetWorkerService(workerId);

        if (worker == null)
        {
            LogInfo($"Worker with ID {workerId} not found.", workerId, LogLevel.Warning);
            return new CommandResult(false, "Worker not found");
        }

        // Forsøg at stoppe arbejdstageren, hvis den ikke allerede er 'Idle'
        if (worker.GetState() != WorkerState.Idle)
        {
            Log.Information($"Worker {workerId} is not idle. Attempting to stop before removal...");
            LogInfo($"Stopping worker {workerId} before removal...", workerId, LogLevel.Warning);
            var stopResult = await worker.StopAsync(); // Delegér stop-logikken til `WorkerService`

            if (!stopResult.Success)
            {
                LogInfo($"Failed to stop worker before removal: {stopResult.Message}", workerId, LogLevel.Error);
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
            LogInfo($"Watchdog event count reset for worker {workerId}.", workerId);
            await SendWorkerEvent(workerId, EventType.Updated);
            return new CommandResult(true, "Watchdog event count reset successfully.");
        }

        LogInfo($"Worker with ID {workerId} not found.", workerId, LogLevel.Warning);
        return new CommandResult(false, "Worker not found.");
    }


    private WorkerState GetWorkerState(string workerId)
    {
        var workerService = GetWorkerService(workerId);
        if (workerService != null) return workerService.GetState();

        return WorkerState.Idle;
    }

    private void SendWorkerDeletedEvent(string workerId)
    {
        Log.Information($"Sending delete event for worker: {workerId}");
        var workerEvent = new WorkerChangeEvent
        {
            Name = "WorkerDeleted",
            Description = "Worker has been deleted",
            Command = "WorkerDeleted",
            WorkerId = workerId,
            EventType = EventType.Deleted,
            Timestamp = DateTime.UtcNow,
            IsEnabled = false,
            WatchdogEventCount = 0,
            ImgWatchdogEnabled = false,
            ImgWatchdogGraceTime = TimeSpan.FromSeconds(10),
            ImgWatchdogInterval = TimeSpan.FromSeconds(2)
        };
        messageQueue.EnqueueMessage(workerEvent);
    }

    private async Task SendWorkerEvent(string workerId, EventType eventType)
    {
        LogInfo($"SendWorkerEvent: Sending worker event: {eventType}", workerId);
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
            LogInfo($"Worker with ID {workerId} not found.", workerId, LogLevel.Warning);
        }
    }
    
    public async Task<WorkerEventWithLogsDto> GetWorkerEventsWithLogsAsync(string workerId)
    {
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var recentEvents = await workerRepository.GetRecentWorkerEventsWithLogsAsync(workerId);

        if (recentEvents == null || !recentEvents.Any())
        {
            throw new InvalidOperationException($"Worker with ID {workerId} not found or has no events.");
        }

        return new WorkerEventWithLogsDto
        {
            WorkerId = workerId,
            Events = recentEvents
        };
    }

    private IWorkerService? GetWorkerService(string workerId)
    {
        return _workers.TryGetValue(workerId, out var worker) ? worker : null;
    }

    private void LogInfo(string message, string workerId, LogLevel logLevel = LogLevel.Information)
    {
        loggerService.LogMessage(new WorkerLogEntry
        {
            WorkerId = workerId,
            Message = $"WorkerManager: {message}",
            LogLevel = logLevel
        });
    }
}