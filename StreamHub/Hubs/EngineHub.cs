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
    
    // SignalR messege fra klient microservice
    public async Task EngineConnected(EngineBaseInfo engineInfo)
    {
        Console.WriteLine($"Engine connected: {engineInfo.EngineId}");
        var engine = engineManager.GetOrAddEngine(engineInfo);
        engine.ConnectionId = Context.ConnectionId;
        await Clients.Caller.SendAsync("EngineAcknowledged");
    }

    // SignalR messege fra klient microservice
    public void ReportWorkers(List<WorkerOut> workers)
    {
        Console.WriteLine($"-----------Reporting workers: {workers.Count}");
        foreach (var worker in workers)
        {
            engineManager.AddOrUpdateWorker(worker);
            Console.WriteLine($"Addddddd Worker: {worker.Name}");
        }
    }

    // SignalR messege fra klient microservice
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

    // SignalR messege fra klient microservice
    public async Task ReceiveLog(LogEntry logMessage)
    {
        if (engineManager.TryGetEngine(logMessage.EngineId, out var engine))
        {
            TimeSpan delay = DateTime.UtcNow - logMessage.Timestamp;
            Console.WriteLine($"Time delay: {delay.TotalMilliseconds} Milliseconds - {logMessage.Timestamp} - {DateTime.UtcNow} - {logMessage.LogSequenceNumber}");
            engine.AddWorkerLog(logMessage.WorkerId, logMessage);
            await hubContext.Clients.Group($"worker-{logMessage.WorkerId}").SendAsync("ReceiveLog", logMessage);
        }
        else
        {
            Console.WriteLine($"ReceiveLog: Engine {logMessage.EngineId} not found");
        }
    }
    
    // SignalR messege fra klient microservice
    public async Task ReceiveImage(ImageData imageData)
    {
        var worker = engineManager.GetWorker(imageData.EngineId, imageData.WorkerId);
        if (worker != null) worker.LastImage = $"data:image/jpeg;base64,{Convert.ToBase64String(imageData.ImageBytes)}";
        await hubContext.Clients.Group("frontendClients").SendAsync("ReceiveImage", imageData, cancellationService.Token);
    }
    
    // Invoke SignalR fra blazor frontend
    public async Task SubscribeToLogs(string workerId)
    {
        Console.WriteLine($"Subscribing to logs for worker {workerId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"worker-{workerId}");
    }

    // Invoke SignalR fra blazor frontend
    public async Task UnsubscribeFromLogs(string workerId)
    {
        Console.WriteLine($"Unsubscribing from logs for worker {workerId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"worker-{workerId}");
    }
    
    // kaldes direkte fra blazor frontend c# kode:         var result = await WorkerService.StopWorkerAsync(engineId, workerId);
    public async Task<CommandResult> StopWorker(Guid engineId, Guid workerId)
    {
        if (engineManager.TryGetEngine(engineId, out var engine))
        {
            // handle no ConnectioonId aka Engine Offfline
            if (string.IsNullOrEmpty(engine.ConnectionId))
            {
                return new CommandResult(false, "Engine Offline");
            }
            
            int timeoutMilliseconds = 5000;
            using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
            using var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationService.Token, timeoutCts.Token);
            try
            {
                Console.WriteLine($"Forwarding StopWorker request for worker {workerId} on engine {engineId}");
                var result = await hubContext.Clients.Client(engine.ConnectionId)
                    .InvokeAsync<CommandResult>("StopWorker", workerId, linkedCts.Token);

                Console.WriteLine($"Received result: {result.Message}");
                return new CommandResult(true, $"Worker {workerId} stopped: {result.Message}");
            }
            catch (OperationCanceledException)
            {
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

    public override async Task OnConnectedAsync()
    {
        var clientType = Context.GetHttpContext()?.Request.Query["clientType"].ToString();

        switch (clientType)
        {
            case "backend":
                await Groups.AddToGroupAsync(Context.ConnectionId, "backendClients");
                Console.WriteLine($"Backend client connected: {Context.ConnectionId}");
                break;
            case "frontend":
                await Groups.AddToGroupAsync(Context.ConnectionId, "frontendClients");
                Console.WriteLine($"Frontend client connected: {Context.ConnectionId}");
                break;
            default:
                Console.WriteLine($"Unknown client connected: {Context.ConnectionId}");
                break;
        }

        await base.OnConnectedAsync();
    }

    public override async Task<Task> OnDisconnectedAsync(Exception? exception)
    {
        var wasEngine = engineManager.RemoveConnection(Context.ConnectionId);
        if (wasEngine)
        {
            await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange",  cancellationService.Token);
            Console.WriteLine($"Engine disconnected: {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        }
        return base.OnDisconnectedAsync(exception);
    }
}