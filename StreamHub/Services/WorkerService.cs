using Common.Models;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Hubs;

namespace StreamHub.Services;

public class WorkerService
{
    private readonly IHubContext<EngineHub> _hubContext;
    private readonly EngineManager _engineManager;
    private readonly CancellationService _cancellationService;

    public WorkerService(IHubContext<EngineHub> hubContext, EngineManager engineManager,
        CancellationService cancellationService)
    {
        _hubContext = hubContext;
        _engineManager = engineManager;
        _cancellationService = cancellationService;
    }

    private async Task<CommandResult> HandleWorkerOperationWithDataAsync(string operation, BaseMessage data,
        int timeoutMilliseconds = 5000)
    {
        if (!_engineManager.TryGetEngine(data.EngineId, out var engine))
        {
            Console.WriteLine($"{operation}: Engine {data.EngineId} not found");
            return new CommandResult(false, "Engine not found.");
        }

        if (engine.ConnectionId == null)
        {
            Console.WriteLine($"{operation}: Engine {data.EngineId} not connected");
            return new CommandResult(false, "Engine not connected.");
        }


        using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(_cancellationService.Token, timeoutCts.Token);
        var msg = "";
        try
        {
            Console.WriteLine($"Forwarding {operation} request with data on engine {data.EngineId}");

            // Invoke the operation that requires more complex data (e.g., CreateWorker)

            var result = await _hubContext.Clients.Client(engine.ConnectionId)
                .InvokeAsync<CommandResult>(operation, data, linkedCts.Token);

            msg = $"{result.Message} Time: {DateTime.Now}";
            return new CommandResult(result.Success, $"{operation}: {result.Message}");
        }
        catch (OperationCanceledException)
        {
            msg = $"Operation canceled for {operation} due to timeout or cancellation.";
            return new CommandResult(false, msg);
        }
        catch (Exception ex)
        {
            msg = $"Error during {operation}: {ex.Message}";
            return new CommandResult(false, msg);
        }
        finally
        {
            Console.WriteLine(msg);
        }
    }


    public async Task<CommandResult> StopWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage(engineId, workerId);
        message.EngineId = engineId;
        return await HandleWorkerOperationWithDataAsync("StopWorker", message);
    }


    public async Task<CommandResult> StartWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage(engineId, workerId);
        message.EngineId = engineId;
        return await HandleWorkerOperationWithDataAsync("StartWorker", message);
    }


    public async Task<CommandResult> RemoveWorkerAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage(engineId, workerId);
        return await HandleWorkerOperationWithDataAsync("RemoveWorker", message);
    }


    public async Task<CommandResult> ResetWatchdogEventCountAsync(Guid engineId, string workerId)
    {
        var message = new WorkerOperationMessage(engineId, workerId);
        return await HandleWorkerOperationWithDataAsync("ResetWatchdogEventCount", message);
    }

    public async Task<CommandResult> CreateWorkerAsync(WorkerCreate workerCreate)
    {
        return await HandleWorkerOperationWithDataAsync("CreateWorker", workerCreate);
    }
}