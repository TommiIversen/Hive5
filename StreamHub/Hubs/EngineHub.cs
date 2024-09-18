using Common.Models;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

public class EngineHub : Hub
{
    private readonly EngineManager _engineManager;
    private readonly CancellationService _cancellationService;
    private readonly IHubContext<EngineHub> _hubContext;

    public EngineHub(EngineManager engineManager, CancellationService cancellationService, IHubContext<EngineHub> hubContext)
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
            try
            {
                Console.WriteLine($"Forwarding StopWorker request for worker {workerId} on engine {engineId}");

                var result = await _hubContext.Clients.Client(engine.ConnectionId)
                    .InvokeAsync<CommandResult>("StopWorker", workerId, _cancellationService.Token);

                Console.WriteLine($"Received result: {result.Message}");
                return new CommandResult(true, $"Worker {workerId} stopped: {result.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Operation canceled for worker {workerId}");
                return new CommandResult(false, $"Operation canceled for worker {workerId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping worker {workerId}: {ex.Message}");
                return new CommandResult(false, $"Failed to stop worker {workerId}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"StopWorker: Engine {engineId} not found");
            return new CommandResult(false, $"Engine {engineId} not found.");
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        _engineManager.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
