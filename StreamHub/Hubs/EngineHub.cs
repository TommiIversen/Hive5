using Common.Models;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

public class EngineHub : Hub
{
    private readonly EngineManager _engineManager;
    private readonly CancellationService _cancellationService;
    private readonly IHubContext<EngineHub> _hubContext;

    public EngineHub(EngineManager engineManager, CancellationService cancellationService,
        IHubContext<EngineHub> hubContext)
    {
        _engineManager = engineManager;
        _cancellationService = cancellationService;
        _hubContext = hubContext;
    }

    public async Task EngineConnected(Guid engineId)
    {
        Console.WriteLine($"Engine connected: {engineId}");
        var engineInfo = _engineManager.GetOrAddEngine(engineId);
        engineInfo.ConnectionId = Context.ConnectionId;
        await Clients.Caller.SendAsync("EngineAcknowledged");
    }

    public async Task ReceiveMetric(Metric metric)
    {
        if (_engineManager.TryGetEngine(metric.EngineId, out var engine))
        {
            engine.LastMetric = metric;
            await _hubContext.Clients.All.SendAsync("UpdateMetric", metric, _cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveMetric: Engine {metric.EngineId} not found");
        }
    }

    public async Task ReceiveLog(LogEntry log)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveLog", log, _cancellationService.Token);
    }

    public async Task ReceiveImage(ImageData imageData)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveImage", imageData, _cancellationService.Token);
    }

    public async Task<CommandResult> StopWorker(Guid engineId, Guid workerId)
    {
        if (_engineManager.TryGetEngine(engineId, out var engine))
        {
            // Definer timeout i millisekunder
            int timeoutMilliseconds = 5000;

            // Opret en cancellation token med timeout
            using (var timeoutCts = new CancellationTokenSource(timeoutMilliseconds))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationService.Token, timeoutCts.Token))
            {
                try
                {
                    Console.WriteLine($"Forwarding StopWorker request for worker {workerId} on engine {engineId}");

                    // Kald InvokeAsync med timeout
                    var result = await _hubContext.Clients.Client(engine.ConnectionId)
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
        _engineManager.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}