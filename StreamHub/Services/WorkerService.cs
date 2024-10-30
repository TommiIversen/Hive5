using Common.DTOs;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Hubs;

namespace StreamHub.Services;

public class WorkerService(
    IHubContext<EngineHub> hubContext,
    EngineManager engineManager,
    CancellationService cancellationService)
{
    private async Task<CommandResult> HandleWorkerOperationWithDataAsync(string operation, WorkerOperationMessage data,
        int timeoutMilliseconds = 5000, bool setProcessing = true)
    {
        CommandResult result;
        var msg = "";
        if (!engineManager.TryGetEngine(data.EngineId, out var engine))
        {
            msg = $"{operation}: Engine {data.EngineId} not found";
            Console.WriteLine(msg);
            return new CommandResult(false, msg);
        }

        if (engine?.ConnectionId == null)
        {
            msg = $"{operation}: Engine {data.EngineId} not connected";
            Console.WriteLine(msg);
            return new CommandResult(false, msg);
        }

        var worker = engineManager.GetWorker(data.EngineId, data.WorkerId);

        if (worker == null && operation != "CreateWorker")
        {
            msg = $"{operation}: Worker {data.WorkerId} not found";
            Console.WriteLine(msg);
            return new CommandResult(false, msg);
        }

        if (setProcessing) worker.IsProcessing = true;

        using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationService.Token, timeoutCts.Token);

        try
        {
            Console.WriteLine($"Forwarding {operation} request with data on engine {data.EngineId}");
            result = await hubContext.Clients.Client(engine.ConnectionId)
                .InvokeAsync<CommandResult>(operation, data, linkedCts.Token);

            msg = $"{result.Message} Time: {DateTime.Now}";
        }
        catch (OperationCanceledException)
        {
            msg = $"Operation canceled for {operation} due to timeout or cancellation.";
            result = new CommandResult(false, msg);
        }
        catch (Exception ex)
        {
            msg = $"Error during {operation}: {ex.Message}";
            result = new CommandResult(false, msg);
        }
        finally
        {
            Console.WriteLine(msg);
            if (setProcessing)
            {
                worker.OperationResult = msg;
                worker.IsProcessing = false;

                // Send nu SignalR-besked for at opdatere UI efter state er sat korrekt
                await hubContext.Clients.Group("frontendClients")
                    .SendAsync($"WorkerLockEvent-{data.EngineId}-{data.WorkerId}", new {worker.IsProcessing, msg});
            }
        }

        return result;
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

    public async Task<CommandResult> CreateWorkerAsync(WorkerCreate workerCreate)
    {
        Console.WriteLine($"CreateWorkerAsync: {workerCreate.WorkerId}");
        return await HandleWorkerOperationWithDataAsync("CreateWorker", workerCreate, setProcessing: false);
    }


    public async Task<CommandResult> EditWorkerAsync(WorkerCreate workerCreate)
    {
        return await HandleWorkerOperationWithDataAsync("EditWorker", workerCreate, setProcessing: true);
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
}