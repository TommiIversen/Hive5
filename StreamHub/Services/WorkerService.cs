using Common.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
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

    public async Task<CommandResult> HandleWorkerOperationAsync(Guid engineId, Guid workerId, string operation,
        int timeoutMilliseconds = 5000)
    {
        var worker = _engineManager.GetWorker(engineId, workerId);
        if (worker == null)
        {
            Console.WriteLine($"{operation}: Worker {workerId} not found");
            return new CommandResult(false, "Worker not found.");
        }

        worker.IsProcessing = true;

        if (_engineManager.TryGetEngine(engineId, out var engine))
        {
            using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
            using var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(_cancellationService.Token, timeoutCts.Token);
            var msg = "";
            try
            {
                Console.WriteLine($"Forwarding {operation} request for worker {workerId} on engine {engineId}");

                // Invoke the specified operation (StartWorker or StopWorker)
                var result = await _hubContext.Clients.Client(engine.ConnectionId)
                    .InvokeAsync<CommandResult>(operation, workerId, linkedCts.Token);

                msg = $"{result.Message} Time: {DateTime.Now}";
                return new CommandResult(true, $"Worker {workerId} {operation.ToLower()}: {result.Message}");
            }
            catch (OperationCanceledException)
            {
                msg = $"Operation canceled for worker {workerId} due to timeout or cancellation.";
                return new CommandResult(false, msg);
            }
            catch (Exception ex)
            {
                msg = $"Error {operation.ToLower()}ing worker {workerId}: {ex.Message}";
                return new CommandResult(false, msg);
            }
            finally
            {
                Console.WriteLine(msg);
                worker.OperationResult = msg;
                worker.IsProcessing = false;
            }
        }
        else
        {
            Console.WriteLine($"{operation}: Engine {engineId} not found");
            return new CommandResult(false, "Engine not found.");
        }
    }


    public async Task<CommandResult> StopWorkerAsync(Guid engineId, Guid workerId, int timeoutMilliseconds = 5000)
    {
        return await HandleWorkerOperationAsync(engineId, workerId, "StopWorker", timeoutMilliseconds);
    }

    public async Task<CommandResult> StartWorkerAsync(Guid engineId, Guid workerId, int timeoutMilliseconds = 5000)
    {
        return await HandleWorkerOperationAsync(engineId, workerId, "StartWorker", timeoutMilliseconds);
    }
    
    public async Task<CommandResult> RemoveWorkerAsync(Guid engineId, Guid workerId, int timeoutMilliseconds = 5000)
    {
        return await HandleWorkerOperationAsync(engineId, workerId, "RemoveWorker", timeoutMilliseconds);
    }
}