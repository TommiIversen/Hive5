using Common.DTOs;
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
    private readonly ILogger<HubWorkerWorkerEventHandlers> _logger;
    private readonly IWorkerManager _workerManager;

    public HubWorkerWorkerEventHandlers(IWorkerManager workerManager, ILogger<HubWorkerWorkerEventHandlers> logger)
    {
        _workerManager = workerManager;
        _logger = logger;
    }

    public void AttachWorkerHandlers(HubConnection hubConnection)
    {
        // Handle StopWorker command asynchronously
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
                var eventsWithLogs = await _workerManager.GetWorkerEventsWithLogsAsync(message.WorkerId);
                return new CommandResult<WorkerEventWithLogsDto>(true, "OK.", eventsWithLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching events with logs for worker {message.WorkerId}: {ex.Message}");
                return new CommandResult<WorkerEventWithLogsDto>(false, $"Error: {ex.Message}");
            }
        });
        
        hubConnection.On("GetWorkerChangeLogs", async (WorkerOperationMessage message) =>
        {
            try
            {
                var changeLogs = await _workerManager.GetWorkerChangeLogsAsync(message.WorkerId);
                return new CommandResult<WorkerChangeLogsDto>(true, "OK.", changeLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching change logs for worker {message.WorkerId}: {ex.Message}");
                return new CommandResult<WorkerChangeLogsDto>(false, $"Error: {ex.Message}");
            }
        });

    }
}