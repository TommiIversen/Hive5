using Common.DTOs.Commands;
using Common.DTOs.Queries;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Hubs;

namespace StreamHub.Services;

public class EngineRequestHandler(
    IHubContext<EngineHub> hubContext,
    IEngineManager engineManager,
    ILogger<EngineManager> logger,
    CancellationService cancellationService)
{


    public async Task<CommandResult<T>> HandleWorkerOperationWithDataAsync<T>(string operation,
        WorkerOperationMessage data,
        int timeoutMilliseconds = 5000, bool setProcessing = true)
    {
        CommandResult<T> result;
        var msg = "";

        if (!engineManager.TryGetEngine(data.EngineId, out var engine))
        {
            msg = $"EngineRequestHandler: {operation}: Engine {data.EngineId} not found";
            LoggerExtensions.LogWarning(logger, msg);
            return new CommandResult<T>(false, msg);
        }

        if (engine?.ConnectionInfo.ConnectionId == null)
        {
            msg = $"EngineRequestHandler: {operation}: Engine {data.EngineId} not connected";
            LoggerExtensions.LogWarning(logger, msg);
            return new CommandResult<T>(false, msg);
        }

        var worker = engineManager.GetWorker(data.EngineId, data.WorkerId);
        if (!string.IsNullOrEmpty(data.WorkerId) && worker == null && operation != "CreateWorker")
        {
            msg = $"{operation}: Worker {data.WorkerId} not found";
            LoggerExtensions.LogWarning(logger, msg);
            return new CommandResult<T>(false, msg);
            
        }

        if (setProcessing && worker != null)
            worker.IsProcessing = true;


        using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationService.Token, timeoutCts.Token);

        try
        {
            LoggerExtensions.LogInformation(logger, $"Forwarding {operation} request with data on engine {data.EngineId}");
            result = await ClientProxyExtensions
                .InvokeAsync<CommandResult<T>>(hubContext.Clients.Client(engine.ConnectionInfo.ConnectionId), operation, data, linkedCts.Token);

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
            LoggerExtensions.LogInformation(logger, msg);
            if (setProcessing && worker != null)
            {
                worker.OperationResult = msg;
                worker.IsProcessing = false;

                // Send SignalR-besked for at opdatere UI efter state er sat korrekt
                await ClientProxyExtensions.SendAsync(hubContext.Clients.Group("frontendClients"), $"WorkerLockEvent-{data.EngineId}-{data.WorkerId}", new { worker.IsProcessing, msg });
            }
        }

        return result;
    }

    public async Task<CommandResult> HandleWorkerOperationWithDataAsync(string operation, WorkerOperationMessage data,
        int timeoutMilliseconds = 5000, bool setProcessing = true)
    {
        return await HandleWorkerOperationWithDataAsync<object>(operation, data, timeoutMilliseconds, setProcessing);
    }
}

public class WorkerService(
    IHubContext<EngineHub> hubContext,
    IEngineManager engineManager,
    ILogger<EngineManager> logger,
    CancellationService cancellationService)
{
    private readonly EngineRequestHandler _engineRequestHandler = new EngineRequestHandler(hubContext, engineManager, logger, cancellationService);
    
    public async Task<CommandResult> StopWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        message.EngineId = engineId;
        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("StopWorker", message);
    }


    public async Task<CommandResult> StartWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        message.EngineId = engineId;
        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("StartWorker", message);
    }


    public async Task<CommandResult> RemoveWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("RemoveWorker", message);
    }


    public async Task<CommandResult> ResetWatchdogEventCountAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage
        {
            WorkerId = workerId,
            EngineId = engineId
        };
        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("ResetWatchdogEventCount", message);
    }

    public async Task<CommandResult> CreateWorkerAsync(WorkerCreateAndEdit workerCreateAndEdit)
    {
        logger.LogInformation($"CreateWorkerAsync: {workerCreateAndEdit.WorkerId}");
        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("CreateWorker", workerCreateAndEdit, setProcessing: false);
    }


    public async Task<CommandResult> EditWorkerAsync(WorkerCreateAndEdit workerCreateAndEdit)
    {
        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("EditWorker", workerCreateAndEdit, setProcessing: true);
    }


    public async Task<CommandResult> EnableDisableWorkerAsync(Guid engineId, string workerId, bool enable)
    {
        var message = new WorkerEnableDisableMessage
        {
            WorkerId = workerId,
            EngineId = engineId,
            Enable = enable
        };

        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("EnableDisableWorker", message);
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
            await _engineRequestHandler.HandleWorkerOperationWithDataAsync<WorkerEventLogCollection>("GetWorkerEventsWithLogs", message,
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
            await _engineRequestHandler.HandleWorkerOperationWithDataAsync<WorkerChangeLog>("GetWorkerChangeLogs", message,
                setProcessing: false);

        var workerChangeLogsDto = result.Data;
        logger.LogInformation($"GetWorkerChangeLogsAsync: {workerChangeLogsDto?.WorkerId}");
        return result;
    }

    public async Task<CommandResult> EditEngineName(EngineEditNameDesc engineUpdate)
    {
        logger.LogInformation($"EditEngineName: {engineUpdate.EngineId}");
        return await _engineRequestHandler.HandleWorkerOperationWithDataAsync("EngineEditName", engineUpdate);
    }
}