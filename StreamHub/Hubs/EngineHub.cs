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
    // SignalR message fra klient microservice
    // public async Task EngineConnected(EngineBaseInfo engineInfo)
    // {
    //     Console.WriteLine($"Engine connected: {engineInfo.EngineId}");
    //     var engine = engineManager.GetOrAddEngine(engineInfo);
    //     engine.ConnectionId = Context.ConnectionId;
    //     await Clients.Caller.SendAsync("EngineAcknowledged", engineInfo.EngineId);
    //     await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
    // }
    
    public async Task EngineConnected(EngineBaseInfo engineInfo)
    {
        // Tjek om engine allerede er forbundet
        if (engineManager.TryGetEngine(engineInfo.EngineId, out var existingEngine) && 
            !string.IsNullOrEmpty(existingEngine.ConnectionId))
        {
            Console.WriteLine($"Engine {engineInfo.EngineId} is already connected. Rejecting new connection.");
            await Clients.Caller.SendAsync("EngineRejected", "Engine is already connected.");
            return; // Afvis forbindelsen, da engine allerede er aktiv
        }

        // Tilføj eller opdater engine og sæt forbindelses ID
        var engine = engineManager.GetOrAddEngine(engineInfo);
        engine.ConnectionId = Context.ConnectionId;
        Console.WriteLine($"Engine {engineInfo.EngineId} connected.");

        // Bekræft forbindelsen til engine
        await Clients.Caller.SendAsync("EngineAcknowledged", engineInfo.EngineId);
        await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
    }
    

    // Synchronize workers ved startup
    public void SynchronizeWorkers(List<WorkerEvent> workers, Guid engineId)
    {
        Console.WriteLine($"-----------SynchronizeWorkers workers: {workers.Count}");
        engineManager.SynchronizeWorkers(workers, engineId);
    }

    public async void ReceiveWorkerEvent(WorkerEvent workerEvent)
    {
        Console.WriteLine($"ReceiveWorkerEvent: {workerEvent.EventType} - {workerEvent.WorkerId} - {workerEvent.Name}");

        if (workerEvent.EventType == WorkerEventType.Deleted)
        {
            engineManager.RemoveWorker(workerEvent.EngineId, workerEvent.WorkerId);
        }

        if (workerEvent.EventType is WorkerEventType.Created or WorkerEventType.Updated)
        {
            engineManager.AddOrUpdateWorker(workerEvent);
        }
        await hubContext.Clients.Group("frontendClients")
            .SendAsync("WorkerEvent", workerEvent, cancellationService.Token);
    }


    // SignalR message fra klient microservice
    public async Task ReceiveMetric(Metric metric)
    {
        if (engineManager.TryGetEngine(metric.EngineId, out var engine))
        {
            engine.AddMetric(metric);
            await hubContext.Clients.All.SendAsync("UpdateMetric", metric, cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveMetric: Engine {metric.EngineId} not found");
        }
    }

    // SignalR message fra klient microservice
    public async Task ReceiveLog(LogEntry logMessage)
    {
        if (engineManager.TryGetEngine(logMessage.EngineId, out var engine))
        {
            // For debugging msg sequence + delay in the system
            //TimeSpan delay = DateTime.UtcNow - logMessage.Timestamp;
            //Console.WriteLine($"Time delay: {delay.TotalMilliseconds} Milliseconds - {logMessage.Timestamp} - {DateTime.UtcNow} - {logMessage.LogSequenceNumber}");
            var wasAdded = engine.AddWorkerLog(logMessage.WorkerId, logMessage);

            if (wasAdded)
            {
                await hubContext.Clients.Group($"worker-{logMessage.EngineId}-{logMessage.WorkerId}").SendAsync("ReceiveLog", logMessage);
            }
            else
            {
                Console.WriteLine($"ReceiveLog: Worker {logMessage.WorkerId} not found");
            }
        }
        else
        {
            Console.WriteLine($"ReceiveLog: Engine {logMessage.EngineId} not found");
        }
    }

    // SignalR message fra klient microservice
    public async Task ReceiveImage(ImageData imageData)
    {
        var worker = engineManager.GetWorker(imageData.EngineId, imageData.WorkerId);

        if (worker != null)
        {
            worker.LastImage = $"data:image/jpeg;base64,{Convert.ToBase64String(imageData.ImageBytes)}";
            await hubContext.Clients.Group("frontendClients")
                .SendAsync("ReceiveImage", imageData, cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveImage: Worker {imageData.WorkerId} not found");
        }
    }

    // SignalR unhandlede message fra klient microservice
    public async Task ReceiveDeadLetter(object deadLetter)
    {
        Console.WriteLine($"Dead letter received: {deadLetter}");
    }

    // Invoke SignalR fra blazor frontend
    public async Task SubscribeToLogs(string workerId, string engineId)
    {
        Console.WriteLine($"Subscribing to logs for worker {workerId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"worker-{engineId}-{workerId}");
    }

    // Invoke SignalR fra blazor frontend
    public async Task UnsubscribeFromLogs(string workerId, string engineId)
    {
        Console.WriteLine($"Unsubscribing from logs for worker {workerId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"worker-{engineId}-{workerId}");
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
            await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
            Console.WriteLine($"Engine disconnected: {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        }

        return base.OnDisconnectedAsync(exception);
    }
}