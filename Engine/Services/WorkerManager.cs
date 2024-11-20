using System.Text.RegularExpressions;
using Common.DTOs.Commands;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Common.DTOs.Queries;
using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Models;
using Engine.Utils;
using Serilog;
using WorkerChangeLog = Common.DTOs.Queries.WorkerChangeLog;

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
    Task<WorkerEventLogCollection> GetWorkerEventsWithLogsAsync(string workerId);
    Task<WorkerChangeLog> GetWorkerChangeLogsAsync(string workerId);
}

public class WorkerManager(
    IMessageQueue messageQueue,
    IRepositoryFactory repositoryFactory,
    ILoggerService loggerService,
    IWorkerServiceFactory workerServiceFactory)
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

            var streamerService = StreamerServiceFactory.CreateStreamerService(
                workerEntity.StreamerType,
                workerEntity.WorkerId,
                workerEntity.Command);

            // Opret WorkerConfiguration fra WorkerEntity med FromEntity metoden
            var workerConfig = WorkerConfiguration.FromEntity(workerEntity);

            // Use factory to create WorkerService
            var workerService = workerServiceFactory.CreateWorkerService(
                workerEntity.WorkerId,
                streamerService,
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

        var workerId = string.IsNullOrWhiteSpace(SanitizeString(workerCreateAndEdit.WorkerId))
            ? Guid.NewGuid().ToString()
            : workerCreateAndEdit.WorkerId;

        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var existingWorker = await workerRepository.GetWorkerByIdAsync(workerId);

        // Hvis arbejderen allerede findes i databasen, returnér null
        if (existingWorker != null)
        {
            LogInfo($"Worker with ID {workerId} already exists in database.",
                workerId,
                LogLevel.Warning);
            return null;
        }

        // Hvis worker allerede findes i databasen og i _workers, returner den eksisterende service
        if (_workers.TryGetValue(workerId, out var workerServiceOut))
        {
            LogInfo($"Worker with ID {workerId} already exists in memory. Returning existing service.", workerId,
                LogLevel.Warning);
            return workerServiceOut;
        }

        LogInfo($"Adding worker to database.. {workerId}", workerId);
        var workerEntity = new WorkerEntity
        {
            WorkerId = workerId,
            Name = workerCreateAndEdit.Name,
            Description = workerCreateAndEdit.Description,
            Command = workerCreateAndEdit.Command,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ImgWatchdogEnabled = workerCreateAndEdit.ImgWatchdogEnabled,
            ImgWatchdogInterval = workerCreateAndEdit.ImgWatchdogInterval,
            ImgWatchdogGraceTime = workerCreateAndEdit.ImgWatchdogGraceTime
        };

        // Tilføj worker til databasen asynkront
        await workerRepository.AddWorkerAsync(workerEntity);

        // Opret en ny streamer service baseret på streamerType       
        var streamerService = StreamerServiceFactory.CreateStreamerService(
            workerEntity.StreamerType,
            workerEntity.WorkerId,
            workerEntity.Command);

        // opret en ny service for workeren
        var workerConfig = WorkerConfiguration.FromEntity(workerEntity);

        var workerService = workerServiceFactory.CreateWorkerService(
            workerEntity.WorkerId,
            streamerService,
            workerConfig);

        _workers[workerId] = workerService;

        await SendWorkerEvent(workerId, ChangeEventType.Created);
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
        await SendWorkerEvent(workerId, ChangeEventType.Updated);

        LogInfo($"Worker {workerId} has been successfully {(enable ? "enabled" : "disabled")}.", workerId);
        return new CommandResult(true, $"Worker {workerId} {(enable ? "enabled" : "disabled")} successfully");
    }


    public async Task<CommandResult> EditWorkerAsync(WorkerCreateAndEdit workerEdit)
    {
        LogInfo($"Editing worker: {workerEdit.WorkerId}", workerEdit.WorkerId);

        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var workerEntity = await workerRepository.GetWorkerByIdAsync(workerEdit.WorkerId);

        if (workerEntity == null)
        {
            LogInfo($"Worker with ID {workerEdit.WorkerId} not found in database.", workerEdit.WorkerId,
                LogLevel.Warning);
            return new CommandResult(false, "Worker not found in database");
        }

        // Brug WorkerChangeDetector til at finde ændringer
        var changeDetector = new WorkerChangeDetector();
        var changes = changeDetector.DetectChanges(workerEntity, workerEdit);

        if (!changes.Any())
        {
            LogInfo("No changes detected", workerEdit.WorkerId);
            return new CommandResult(true, "No changes detected");
        }

        // Opdater workerEntity med nye værdier
        workerEntity.Name = workerEdit.Name;
        workerEntity.Description = workerEdit.Description;
        workerEntity.Command = workerEdit.Command ?? string.Empty;
        workerEntity.IsEnabled = workerEdit.IsEnabled;
        workerEntity.ImgWatchdogEnabled = workerEdit.ImgWatchdogEnabled;
        workerEntity.ImgWatchdogInterval = workerEdit.ImgWatchdogInterval;
        workerEntity.ImgWatchdogGraceTime = workerEdit.ImgWatchdogGraceTime;
        workerEntity.UpdatedAt = DateTime.UtcNow;

        var streamerChanged = workerEntity.StreamerType != workerEdit.StreamerType;
        workerEntity.StreamerType = workerEdit.StreamerType;

        await workerRepository.UpdateWorkerAsync(workerEntity);

        // Tilføj ændringerne til WorkerChangeLog
        await workerRepository.AddWorkerChangeLogsAsync(changes);

        // Ekstra handlinger baseret på specifikke ændringer (kommandoændring, watchdogændringer)
        var commandChanged = changes.Any(c => c.ChangeDescription == "Command changed");
        var watchdogChanged = changes.Any(c => c.ChangeDescription == "Watchdog settings changed");

        if (watchdogChanged)
        {
            var workerService = GetWorkerService(workerEdit.WorkerId);
            workerService?.UpdateWatchdogSettingsAsync(workerEdit.ImgWatchdogEnabled, workerEdit.ImgWatchdogInterval,
                workerEdit.ImgWatchdogGraceTime);
        }

        if (commandChanged)
        {
            var workerService = GetWorkerService(workerEdit.WorkerId);
            if (workerService != null)
            {
                LogInfo($"Restarting worker {workerEdit.WorkerId} due to command change.", workerEdit.WorkerId);
                await workerService.StopAsync();
                workerService.SetGstCommand(workerEdit.Command ?? string.Empty);
                var result = await workerService.StartAsync();
                if (!result.Success)
                {
                    LogInfo($"Failed to restart worker {workerEdit.WorkerId}: {result.Message}", workerEdit.WorkerId,
                        LogLevel.Error);
                    return new CommandResult(false, $"Worker updated but failed to restart: {result.Message}");
                }
            }
        }

        // Handle streamer change
        if (streamerChanged)
        {
            var workerService = GetWorkerService(workerEdit.WorkerId);
            if (workerService != null)
            {
                LogInfo($"Changing streamer type for worker {workerEdit.WorkerId}.", workerEdit.WorkerId);

                await workerService.StopAsync();
                var newStreamerService = StreamerServiceFactory.CreateStreamerService(workerEdit.StreamerType,
                    workerEdit.WorkerId, workerEdit.Command ?? string.Empty);
                workerService.ReplaceStreamer(newStreamerService);
                var result = await workerService.StartAsync();

                if (!result.Success)
                {
                    LogInfo($"Failed to restart worker {workerEdit.WorkerId} with new streamer: {result.Message}",
                        workerEdit.WorkerId, LogLevel.Error);
                    return new CommandResult(false,
                        $"Worker updated but failed to restart with new streamer: {result.Message}");
                }
            }
        }

        await SendWorkerEvent(workerEdit.WorkerId, ChangeEventType.Updated);
        LogInfo($"Worker updated successfully: {string.Join(", ", changes.Select(c => c.ChangeDescription))}",
            workerEdit.WorkerId);

        return new CommandResult(true,
            $"Worker updated successfully: {string.Join(", ", changes.Select(c => c.ChangeDescription))}");
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
            await SendWorkerEvent(workerId, ChangeEventType.Updated);
            return new CommandResult(true, "Watchdog event count reset successfully.");
        }

        LogInfo($"Worker with ID {workerId} not found.", workerId, LogLevel.Warning);
        return new CommandResult(false, "Worker not found.");
    }

    public async Task<WorkerEventLogCollection> GetWorkerEventsWithLogsAsync(string workerId)
    {
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var recentEvents = await workerRepository.GetRecentWorkerEventsWithLogsAsync(workerId);

        if (recentEvents == null || !recentEvents.Any())
            throw new InvalidOperationException($"Worker with ID {workerId} not found or has no events.");

        return new WorkerEventLogCollection
        {
            Events = recentEvents
        };
    }

    public async Task<WorkerChangeLog> GetWorkerChangeLogsAsync(string workerId)
    {
        var workerRepository = repositoryFactory.CreateWorkerRepository();
        var changeLogs = await workerRepository.GetWorkerChangeLogAsync(workerId);

        if (changeLogs == null || !changeLogs.Any())
            throw new InvalidOperationException($"Worker with ID {workerId} not found or has no change logs.");
        Console.WriteLine("WorkerChangeLogs: " + changeLogs.Count);

        return new WorkerChangeLog
        {
            WorkerId = workerId,
            Changes = changeLogs.Select(log => new WorkerChangeLogEntry
            {
                ChangeTimestamp = log.ChangeTimestamp,
                ChangeDescription = log.ChangeDescription,
                ChangeDetails = log.ChangeDetails
            }).ToList()
        };
    }

    private static string SanitizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty; // Returnér tom streng, hvis input er null eller kun whitespace

        // Trim whitespace i starten og slutningen
        input = input.Trim();

        // Erstat mellemrum og specialtegn i strengen med underscore
        var sanitized = Regex.Replace(input, @"\s+", "_"); // Erstatter alle mellemrum med underscore
        sanitized = Regex.Replace(sanitized, @"[^\w]", "_"); // Erstatter specialtegn med underscore

        // Fjern eventuelle ekstra underscores i træk
        sanitized = Regex.Replace(sanitized, @"_+", "_");

        return sanitized;
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
            ChangeEventType = ChangeEventType.Deleted,
            Timestamp = DateTime.UtcNow,
            IsEnabled = false,
            WatchdogEventCount = 0,
            ImgWatchdogEnabled = false,
            ImgWatchdogGraceTime = TimeSpan.FromSeconds(10),
            ImgWatchdogInterval = TimeSpan.FromSeconds(2),
            Streamer = ""
        };
        messageQueue.EnqueueMessage(workerEvent);
    }

    private async Task SendWorkerEvent(string workerId, ChangeEventType changeEventType)
    {
        LogInfo($"SendWorkerEvent: Sending worker event: {changeEventType}", workerId);
        var workerService = GetWorkerService(workerId);

        if (workerService != null)
        {
            var workerRepository = repositoryFactory.CreateWorkerRepository();
            var workerEntity = await workerRepository.GetWorkerByIdAsync(workerId);
            if (workerEntity != null)
            {
                var workerEvent = workerEntity.ToWorkerEvent(workerService.GetState(), changeEventType);
                messageQueue.EnqueueMessage(workerEvent);
            }
        }
        else
        {
            LogInfo($"Worker with ID {workerId} not found.", workerId, LogLevel.Warning);
        }
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
            LogLevel = logLevel,
            LogTimestamp = DateTime.UtcNow
        });
    }
}