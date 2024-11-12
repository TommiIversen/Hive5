using Common.DTOs.Commands;
using Common.DTOs.Queries;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Hubs;

namespace StreamHub.Services;

public class WorkerService(
    IHubContext<EngineHub> hubContext,
    IEngineManager engineManager,
    ILogger<EngineManager> logger,
    CancellationService cancellationService)
{
    private async Task<CommandResult<T>> HandleWorkerOperationWithDataAsync<T>(string operation,
        WorkerOperationMessage data,
        int timeoutMilliseconds = 5000, bool setProcessing = true)
    {
        CommandResult<T> result;
        var msg = "";

        if (!engineManager.TryGetEngine(data.EngineId, out var engine))
        {
            msg = $"{operation}: Engine {data.EngineId} not found";
            logger.LogWarning(msg);
            return new CommandResult<T>(false, msg);
        }

        if (engine?.ConnectionInfo.ConnectionId == null)
        {
            msg = $"{operation}: Engine {data.EngineId} not connected";
            logger.LogWarning(msg);
            return new CommandResult<T>(false, msg);
        }

        var worker = engineManager.GetWorker(data.EngineId, data.WorkerId);
        if (worker == null && operation != "CreateWorker")
        {
            msg = $"{operation}: Worker {data.WorkerId} not found";
            logger.LogWarning(msg);
            return new CommandResult<T>(false, msg);
        }

        if (setProcessing) worker.IsProcessing = true;

        using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationService.Token, timeoutCts.Token);

        try
        {
            logger.LogInformation($"Forwarding {operation} request with data on engine {data.EngineId}");
            result = await hubContext.Clients.Client(engine.ConnectionInfo.ConnectionId)
                .InvokeAsync<CommandResult<T>>(operation, data, linkedCts.Token);

            msg = $"{result.Message} Time: {DateTime.Now}";
        }
        catch (OperationCanceledException)
        {
            msg = $"Operation canceled for {operation} due to timeout or cancellation.";
            result = new CommandResult<T>(false, msg);
        }
        catch (Exception ex)
        {
            msg = $"Error during {operation}: {ex.Message}";
            result = new CommandResult<T>(false, msg);
        }
        finally
        {
            logger.LogInformation(msg);
            if (setProcessing)
            {
                worker.OperationResult = msg;
                worker.IsProcessing = false;

                // Send SignalR-besked for at opdatere UI efter state er sat korrekt
                await hubContext.Clients.Group("frontendClients")
                    .SendAsync($"WorkerLockEvent-{data.EngineId}-{data.WorkerId}", new { worker.IsProcessing, msg });
            }
        }

        return result;
    }

    // Ikke-generisk version, der kalder den generiske med `T = object`
    private async Task<CommandResult> HandleWorkerOperationWithDataAsync(string operation, WorkerOperationMessage data,
        int timeoutMilliseconds = 5000, bool setProcessing = true)
    {
        return await HandleWorkerOperationWithDataAsync<object>(operation, data, timeoutMilliseconds, setProcessing);
    }

    public async Task<CommandResult> StopWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        message.EngineId = engineId;
        return await HandleWorkerOperationWithDataAsync("StopWorker", message);
    }


    public async Task<CommandResult> StartWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        message.EngineId = engineId;
        return await HandleWorkerOperationWithDataAsync("StartWorker", message);
    }


    public async Task<CommandResult> RemoveWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        return await HandleWorkerOperationWithDataAsync("RemoveWorker", message);
    }


    public async Task<CommandResult> ResetWatchdogEventCountAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        return await HandleWorkerOperationWithDataAsync("ResetWatchdogEventCount", message);
    }

    public async Task<CommandResult> CreateWorkerAsync(WorkerCreateAndEdit workerCreateAndEdit)
    {
        logger.LogInformation($"CreateWorkerAsync: {workerCreateAndEdit.WorkerId}");
        return await HandleWorkerOperationWithDataAsync("CreateWorker", workerCreateAndEdit, setProcessing: false);
    }


    public async Task<CommandResult> EditWorkerAsync(WorkerCreateAndEdit workerCreateAndEdit)
    {
        return await HandleWorkerOperationWithDataAsync("EditWorker", workerCreateAndEdit, setProcessing: true);
    }


    public async Task<CommandResult> EnableDisableWorkerAsync(Guid engineId, string workerId, bool enable)
    {
        var message = new WorkerEnableDisableMessage
        {
            WorkerId = workerId,
            EngineId = engineId,
            Enable = enable
        };

        return await HandleWorkerOperationWithDataAsync("EnableDisableWorker", message);
    }

    public async Task<CommandResult<WorkerEventLogCollection>> GetWorkerEventsWithLogsAsync(Guid engineId,
        string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };

        var result =
            await HandleWorkerOperationWithDataAsync<WorkerEventLogCollection>("GetWorkerEventsWithLogs", message,
                setProcessing: false);

        return result;
    }

    public async Task<CommandResult<WorkerChangeLog>> GetWorkerChangeLogsAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };

        var result =
            await HandleWorkerOperationWithDataAsync<WorkerChangeLog>("GetWorkerChangeLogs", message,
                setProcessing: false);

        var workerChangeLogsDto = result.Data;
        logger.LogInformation($"GetWorkerChangeLogsAsync: {workerChangeLogsDto?.WorkerId}");
        return result;
    }
}