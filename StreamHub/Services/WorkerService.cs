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

    public WorkerService(IHubContext<EngineHub> hubContext, EngineManager engineManager, CancellationService cancellationService)
    {
        _hubContext = hubContext;
        _engineManager = engineManager;
        _cancellationService = cancellationService;
    }

    public async Task<CommandResult> StopWorkerAsync(Guid engineId, Guid workerId, int timeoutMilliseconds = 5000)
    {
        var worker = _engineManager.GetWorker(engineId, workerId);
        if (worker == null)
        {
            Console.WriteLine($"StopWorker: Worker {workerId} not found");
            return new CommandResult(false, "Worker not found.");
        }
        worker.IsProcessing = true;
        
        if (_engineManager.TryGetEngine(engineId, out var engine))
        {
            using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationService.Token, timeoutCts.Token);
            var msg = "";
            try
            {
                Console.WriteLine($"Forwarding StopWorker request for worker {workerId} on engine {engineId}");

                var result = await _hubContext.Clients.Client(engine.ConnectionId)
                    .InvokeAsync<CommandResult>("StopWorker", workerId, linkedCts.Token);

                msg = $"{result.Message} Time: {DateTime.Now}";
                return new CommandResult(true, $"Worker {workerId} stopped: {result.Message}");
            }
            catch (OperationCanceledException)
            {
                msg = $"Operation canceled for worker {workerId} due to timeout or cancellation.";
                return new CommandResult(false, msg);
            }
            catch (Exception ex)
            {
                msg = $"Error stopping worker {workerId}: {ex.Message}";
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
            Console.WriteLine($"StopWorker: Engine {engineId} not found");
            return new CommandResult(false, "Engine not found.");
        }
    }
}
