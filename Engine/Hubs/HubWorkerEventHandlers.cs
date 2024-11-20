using Common.DTOs.Commands;
using Common.DTOs.Queries;
using Engine.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace Engine.Hubs;

public interface IHubWorkerEventHandlers
{
    void AttachWorkerHandlers(HubConnection hubConnection);
}

public class HubWorkerWorkerEventHandlers : IHubWorkerEventHandlers
{
    private readonly IEngineService _engineService;
    private readonly ILogger<HubWorkerWorkerEventHandlers> _logger;
    private readonly IWorkerManager _workerManager;

    public HubWorkerWorkerEventHandlers(IWorkerManager workerManager, IEngineService engineService,
        ILogger<HubWorkerWorkerEventHandlers> logger)
    {
        _workerManager = workerManager;
        _engineService = engineService;
        _logger = logger;
    }

    public void AttachWorkerHandlers(HubConnection hubConnection)
    {
        hubConnection.On("StopWorker", async (WorkerOperationMessage message) =>
        {
            var commandResult = await _workerManager.StopWorkerAsync(message.WorkerId);
            return commandResult;
        });

        hubConnection.On("StartWorker", async (WorkerOperationMessage message) =>
        {
            var commandResult = await _workerManager.StartWorkerAsync(message.WorkerId);
            return commandResult;
        });

        // Handle RemoveWorker command asynchronously
        hubConnection.On("RemoveWorker", async (WorkerOperationMessage message) =>
        {
            var commandResult = await _workerManager.RemoveWorkerAsync(message.WorkerId);
            return commandResult;
        });

        hubConnection.On("ResetWatchdogEventCount", async (WorkerOperationMessage message) =>
        {
            var commandResult = await _workerManager.ResetWatchdogEventCountAsync(message.WorkerId);
            return commandResult;
        });

        hubConnection.On("EnableDisableWorker", async (WorkerEnableDisableMessage message) =>
        {
            var commandResult = await _workerManager.EnableDisableWorkerAsync(message.WorkerId, message.Enable);
            return commandResult;
        });

        hubConnection.On("EditWorker", async (WorkerCreateAndEdit workerEdit) =>
        {
            var commandResult = await _workerManager.EditWorkerAsync(workerEdit);
            return commandResult;
        });

        hubConnection.On("CreateWorker", async (WorkerCreateAndEdit workerCreate) =>
        {
            var workerManager = await _workerManager.AddWorkerAsync(workerCreate.EngineId, workerCreate);
            if (workerManager == null)
                return new CommandResult(false, $"Worker with ID {workerCreate.WorkerId} already exists.");
            var result = await _workerManager.StartWorkerAsync(workerManager.WorkerId);
            return result;
        });

        hubConnection.On("GetWorkerEventsWithLogs", async (WorkerOperationMessage message) =>
        {
            try
            {
                _logger.LogInformation($"GetWorkerEventsWithLogs: {message.WorkerId}");
                var eventsWithLogs = await _workerManager.GetWorkerEventsWithLogsAsync(message.WorkerId);
                return new CommandResult<WorkerEventLogCollection>(true, "OK.", eventsWithLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching events with logs for worker {message.WorkerId}: {ex.Message}");
                return new CommandResult<WorkerEventLogCollection>(false, $"Error: {ex.Message}");
            }
        });

        hubConnection.On("GetWorkerChangeLogs", async (WorkerOperationMessage message) =>
        {
            try
            {
                Console.WriteLine($"Try GetWorkerChangeLogs: {message.WorkerId}");
                var changeLogs = await _workerManager.GetWorkerChangeLogsAsync(message.WorkerId);
                var result = new CommandResult<WorkerChangeLog>(true, "OK.", changeLogs);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching change logs for worker {message.WorkerId}: {ex.Message}");
                return new CommandResult<WorkerChangeLog>(false, $"Error: {ex.Message}");
            }
        });
    }
}