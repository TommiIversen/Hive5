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
    public async Task EngineConnected(EngineBaseInfo engineInfo)
    {
        Console.WriteLine($"Engine connected: {engineInfo.EngineId}");
        var engine = engineManager.GetOrAddEngine(engineInfo);
        engine.ConnectionId = Context.ConnectionId;
        await Clients.Caller.SendAsync("EngineAcknowledged", engineInfo.EngineId);
    }

    // SignalR message fra Engine som reportere sine workers
    public void ReportWorkers(List<WorkerOut> workers)
    {
        Console.WriteLine($"-----------Reporting workers: {workers.Count}");
        foreach (var worker in workers)
        {
            engineManager.AddOrUpdateWorker(worker);
            Console.WriteLine($"Addddddd Worker: {worker.Name}");
        }
    }

    public void ReceiveWorkerEvent(WorkerEvent workerEvent)
    {
        Console.WriteLine($"ReceiveWorkerEvent: {workerEvent.EventType} - {workerEvent.WorkerId}");

        if (workerEvent.EventType == WorkerEventType.Deleted)
        {
            engineManager.RemoveWorker(workerEvent.EngineId, workerEvent.WorkerId);
        }

        if (workerEvent.EventType is WorkerEventType.Created or WorkerEventType.Updated)
        {
            engineManager.AddOrUpdateWorker(workerEvent);
        }
    }


    // SignalR message fra klient microservice
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
                await hubContext.Clients.Group($"worker-{logMessage.WorkerId}").SendAsync("ReceiveLog", logMessage);
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


    public async Task ReceiveDeadLetter(object deadLetter)
    {
        Console.WriteLine($"Dead letter received: {deadLetter}");
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