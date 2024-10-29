using Common.DTOs;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

namespace StreamHub.Hubs;

public class EngineHub(
    EngineManager engineManager,
    CancellationService cancellationService,
    IHubContext<EngineHub> hubContext)
    : Hub
{

    public async Task<bool> RegisterEngineConnection(EngineBaseInfo engineInfo)
    {
        // Tjek om engine allerede er forbundet
        if (engineManager.TryGetEngine(engineInfo.EngineId, out var existingEngine) &&
            existingEngine != null &&
            !string.IsNullOrEmpty(existingEngine.ConnectionId))
        {
            Console.WriteLine($"RegisterEngineConnection Engine {engineInfo.EngineId} is already connected. Rejecting new connection.");
            return false; // Afvis forbindelsen, da engine allerede er aktiv
        }

        Console.WriteLine($"RegisterEngineConnection Engine {engineInfo.EngineId} connected.");
        
        // Tilføj eller opdater engine og sæt forbindelses ID
        var engine = engineManager.GetOrAddEngine(engineInfo);
        engine.ConnectionId = Context.ConnectionId;
        engine.OnlineSince = DateTime.UtcNow;
        engine.BaseInfo = engineInfo;

        // Få flere oplysninger om forbindelsen
        var httpContext = Context.GetHttpContext();
        if (httpContext != null)
        {
            engine.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            engine.Port = httpContext.Connection.RemotePort;
            engine.LocalPort = httpContext.Connection.LocalPort;
            engine.TransportType = Context.Features.Get<IHttpTransportFeature>()?.TransportType.ToString();
        
            Console.WriteLine($"Connected from IP: {engine.IpAddress}, Port: {engine.Port}, Transport: {engine.TransportType}, Time: {engine.OnlineSince}");
        }

        // Tilføj engine til backendClients-gruppen efter godkendelse
        await Groups.AddToGroupAsync(Context.ConnectionId, "backendClients");
        await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
        return true; // Forbindelsen er godkendt
    }

    // SignalR hander for at fjerne en hub URL
    public async Task RemoveHubUrl(string hubUrl, Guid engineId)
    {
        Console.WriteLine($"Removing hub connection for URL: {hubUrl}");
        
        // Tjek om engine allerede er forbundet
        if (engineManager.TryGetEngine(engineId, out var existingEngine) && 
            string.IsNullOrEmpty(existingEngine?.ConnectionId))
        {
            Console.WriteLine($"Engine is not connected.");
            //return new CommandResult(false, "Engine is not connected");
        }

        using var timeoutCts = new CancellationTokenSource(5000);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationService.Token, timeoutCts.Token);
        
        if (existingEngine is {ConnectionId: not null})
        {
            CommandResult result;
            result = await hubContext.Clients.Client(existingEngine.ConnectionId)
                .InvokeAsync<CommandResult>("RemoveHubConnection", hubUrl, linkedCts.Token);
            Console.WriteLine($"RemoveHubUrlRemoveHubUrlRemoveHubUrlRemoveHubUrl Result: {result.Message}");
                
        }

        await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
        //return new CommandResult(true, "Hub URL removed");
    }

    // Synchronize workers ved startup
    public async void SynchronizeWorkers(List<WorkerEvent> workers, Guid engineId)
    {
        Console.WriteLine($"-----------SynchronizeWorkers workers: {workers.Count}");
        engineManager.SynchronizeWorkers(workers, engineId);
        await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
    }
    
    public async void SendSystemInfo(SystemInfoModel systemInfo)
    {
        Console.WriteLine($"SendSystemInfo: {systemInfo.OsName} - {systemInfo.OSVersion} - {systemInfo.Architecture} - {systemInfo.Uptime} - {systemInfo.ProcessCount} - {systemInfo.Platform}");
        
        
        if (engineManager.TryGetEngine(systemInfo.EngineId, out var engine))
        {
            engine.SystemInfo = systemInfo;
            await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"SendSystemInfo: Engine {systemInfo.EngineId} not found");
        }
    }

    
    // ReceiveEngineEvent like ReceiveWorkerEvent
    public async void ReceiveEngineEvent(EngineEvent engineEvent)
    {
        Console.WriteLine($"GOTTTTT EngineEvent: {engineEvent.EventType} - {engineEvent.EngineId}");
        switch (engineEvent.EventType)
        {
            // case EventType.Deleted:
            //     engineManager.RemoveEngine(engineEvent.EngineId);
            //     await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
            //     break;
            // case EventType.Created:
            //     engineManager.AddOrUpdateEngine(engineEvent);
            //     await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
            //     break;
            case EventType.Updated:
                engineManager.UpdateBaseInfo(engineEvent);
                await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
                break;
        }
    }
    
    
    public async void ReceiveWorkerEvent(WorkerEvent workerEvent)
    {
        switch (workerEvent.EventType)
        {
            case EventType.Deleted:
                engineManager.RemoveWorker(workerEvent.EngineId, workerEvent.WorkerId);
                await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
                break;
            case EventType.Created:
                engineManager.AddOrUpdateWorker(workerEvent);
                await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
                break;
            case EventType.Updated:
                engineManager.AddOrUpdateWorker(workerEvent);
                string workerEventTopic = $"WorkerEvent-{workerEvent.EngineId}-{workerEvent.WorkerId}";
                
                await hubContext.Clients.Group("frontendClients")
                    .SendAsync(workerEventTopic, workerEvent, cancellationService.Token);
                break;
        }
    }


    // SignalR message fra klient microservice
    public async Task ReceiveMetric(Metric metric)
    {
        if (engineManager.TryGetEngine(metric.EngineId, out var engine))
        {
            engine?.AddMetric(metric);
            
            // first metric ?
            if (engine?.LastMetric == null)
            {
                await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
            }
            
            await hubContext.Clients.All.SendAsync($"UpdateMetric-{metric.EngineId}", metric, cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveMetric: Engine {metric.EngineId} not found");
        }
    }

    // SignalR message fra klient microservice
    public async Task ReceiveWorkerLog(WorkerLogEntry workerLogMessage)
    {
        if (engineManager.TryGetEngine(workerLogMessage.EngineId, out var engine))
        {
            // For debugging msg sequence + delay in the system
            //TimeSpan delay = DateTime.UtcNow - logMessage.Timestamp;
            //Console.WriteLine($"Time delay: {delay.TotalMilliseconds} Milliseconds - {logMessage.Timestamp} - {DateTime.UtcNow} - {logMessage.LogSequenceNumber}");
            var wasAdded = engine != null && engine.AddWorkerLog(workerLogMessage.WorkerId, workerLogMessage);

            if (wasAdded)
            {
                await hubContext.Clients.Group($"worker-{workerLogMessage.EngineId}-{workerLogMessage.WorkerId}").SendAsync("ReceiveWorkerLog", workerLogMessage);
            }
            else
            {
                Console.WriteLine($"ReceiveLog: Worker {workerLogMessage.WorkerId} not found");
            }
        }
        else
        {
            Console.WriteLine($"ReceiveLog: Engine {workerLogMessage.EngineId} not found");
        }
    }
    
    public async Task ReceiveEngineLog(EngineLogEntry engineLog)
    {
        Console.WriteLine($"ReceiveEngineLog: {engineLog.Message}");
        if (engineManager.TryGetEngine(engineLog.EngineId, out var engine))
        {
            engine?.AddEngineLog(engineLog);
            await hubContext.Clients.Group($"enginelog-{engineLog.EngineId}").SendAsync("ReceiveEngineLog", engineLog);
        }
        else
        {
            Console.WriteLine($"ReceiveLog: Engine {engineLog.EngineId} not found");
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
                .SendAsync($"ReceiveImage-{imageData.EngineId}-{imageData.WorkerId}", imageData, cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveImage: Worker {imageData.WorkerId} not found");
        }
    }

    // SignalR unhandlede message fra klient microservice
    public async Task ReceiveDeadLetter(string deadLetter)
    {
        Console.WriteLine($"Dead letter received: {deadLetter}");
    }

    // Invoke SignalR fra blazor frontend
    public async Task SubscribeToWorkerLogs(string workerId, string engineId)
    {
        Console.WriteLine($"Subscribing to logs for worker {workerId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"worker-{engineId}-{workerId}");
    }
    

    public async Task SubscribeToEngineLogs(Guid engineId)
    {
        Console.WriteLine($"Subscribing to logs for engine {engineId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"enginelog-{engineId}");
    }
    
    public async Task UnsubscribeFromEngineLogs(Guid engineId)
    {
        Console.WriteLine($"Unsubscribing to logs for engine {engineId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"enginelog-{engineId}");
    }

    // Invoke SignalR fra blazor frontend
    public async Task UnsubscribeFromWorkerLogs(string workerId, string engineId)
    {
        Console.WriteLine($"Unsubscribing from logs for worker {workerId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"worker-{engineId}-{workerId}");
    }
    
    // Invoke SignalR fra blazor frontend - remove engine
    public async Task RemoveEngine(Guid engineId)
    {
        Console.WriteLine($"Removing engine {engineId}");
        engineManager.RemoveEngine(engineId);
        await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
    }
    
    


    public override async Task OnConnectedAsync()
    {
        var clientType = Context.GetHttpContext()?.Request.Query["clientType"].ToString();
        switch (clientType)
        {
            case "backend":
                Console.WriteLine($"Backend client attempting to connect: {Context.ConnectionId}");
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