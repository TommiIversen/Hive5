using Common.Models;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

namespace StreamHub.Hubs;

public class EngineHub(
    EngineManager engineManager,
    CancellationService cancellationService,
    IHubContext<EngineHub> hubContext)
    : Hub
{
    public async Task EngineConnected(Guid engineId)
    {
        Console.WriteLine($"Engine connected: {engineId}");
        var engineInfo = engineManager.GetOrAddEngine(engineId);
        engineInfo.ConnectionId = Context.ConnectionId;
        await Clients.Caller.SendAsync("EngineAcknowledged");
    }

    public async Task ReceiveMetric(Metric metric)
    {
        if (engineManager.TryGetEngine(metric.EngineId, out var engine))
        {
            engine.LastMetric = metric;
            await hubContext.Clients.All.SendAsync("UpdateMetric", metric, cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveMetric: Engine {metric.EngineId} not found");
        }
    }

    public async Task ReceiveLog(LogEntry log)
    {
        if (engineManager.TryGetEngine(log.EngineId, out var engine))
        {
            engine.AddWorkerLog(log.WorkerId, log.Message);
            await hubContext.Clients.All.SendAsync("ReceiveLog", log);
        }
    }

    public async Task ReceiveImage(ImageData imageData)
    {
        var worker = engineManager.GetWorker(imageData.EngineId, imageData.WorkerId);
        if (worker != null) worker.LastImage = $"data:image/jpeg;base64,{Convert.ToBase64String(imageData.ImageBytes)}";
        await hubContext.Clients.All.SendAsync("ReceiveImage", imageData, cancellationService.Token);
    }

    public async Task<CommandResult> StopWorker(Guid engineId, Guid workerId)
    {
        if (engineManager.TryGetEngine(engineId, out var engine))
        {
            // Definer timeout i millisekunder
            int timeoutMilliseconds = 5000;

            // Opret en cancellation token med timeout
            using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationService.Token, timeoutCts.Token);
            try
            {
                Console.WriteLine($"Forwarding StopWorker request for worker {workerId} on engine {engineId}");

                // Kald InvokeAsync med timeout
                var result = await hubContext.Clients.Client(engine.ConnectionId)
                    .InvokeAsync<CommandResult>("StopWorker", workerId, linkedCts.Token);

                Console.WriteLine($"Received result: {result.Message}");
                return new CommandResult(true, $"Worker {workerId} stopped: {result.Message}");
            }
            catch (OperationCanceledException)
            {
                // Generisk timeout/annullering fejlmeddelelse
                Console.WriteLine($"Operation canceled for worker {workerId} due to timeout or cancellation.");
                return new CommandResult(false, "Operation canceled or timed out.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping worker {workerId}: {ex.Message}");
                return new CommandResult(false, "Failed to stop worker.");
            }
        }
        else
        {
            Console.WriteLine($"StopWorker: Engine {engineId} not found");
            return new CommandResult(false, "Engine not found.");
        }
    }


    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        engineManager.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}