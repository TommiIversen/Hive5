using Common.DTOs;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

namespace StreamHub.Hubs;

public class EngineHub(
    EngineManager engineManager,
    CancellationService cancellationService,
    IHubContext<EngineHub> hubContext,
    FrontendHandlers frontendHandlers,
    BackendHandlers backendHandlers)
    : Hub
{
    // Frontend handlers
    public async Task RemoveEngine(Guid engineId)
    {
        await frontendHandlers.RemoveEngine(engineId);
    }

    public async Task RemoveHubUrl(string hubUrl, Guid engineId)
    {
        await frontendHandlers.RemoveHubUrl(hubUrl, engineId);
    }

    public async Task SubscribeToEngineLogs(Guid engineId)
    {
        await frontendHandlers.SubscribeToEngineLogs(engineId, Context.ConnectionId);
    }

    public async Task ReturnWorkerEventsWithLogs(WorkerEventWithLogsDto workerId)
    {
        Console.WriteLine($"øøøøøøøøøøøøøøReturnWorkerEventsWithLogs: {workerId.WorkerId}");
        //workerId.Events.ForEach(e => Console.WriteLine($"Event: {e.EventMessage}"));
    }

    public async Task UnsubscribeFromEngineLogs(Guid engineId)
    {
        await frontendHandlers.UnsubscribeFromEngineLogs(engineId, Context.ConnectionId);
    }

    public async Task SubscribeToWorkerLogs(string workerId, string engineId)
    {
        await frontendHandlers.SubscribeToWorkerLogs(workerId, engineId, Context.ConnectionId);
    }

    public async Task UnsubscribeFromWorkerLogs(string workerId, string engineId)
    {
        await frontendHandlers.UnsubscribeFromWorkerLogs(workerId, engineId, Context.ConnectionId);
    }


    // Backend engine handlers
    public async Task<bool> RegisterEngineConnection(EngineBaseInfo engineInfo)
    {
        return await backendHandlers.RegisterEngineConnection(engineInfo, Context);
    }

    public async Task SynchronizeWorkers(List<WorkerChangeEvent> workers, Guid engineId)
    {
        await backendHandlers.SynchronizeWorkers(workers, engineId);
    }


    public async Task ReceiveEngineSystemInfo(SystemInfoModel systemInfo)
    {
        await backendHandlers.ReceiveEngineSystemInfo(systemInfo);
    }


    public async Task ReceiveEngineEvent(EngineEvent engineEvent)
    {
        await backendHandlers.ReceiveEngineEvent(engineEvent);
    }


    public async Task ReceiveWorkerEvent(WorkerChangeEvent workerChangeEvent)
    {
        await backendHandlers.ReceiveWorkerEvent(workerChangeEvent);
    }


    public async Task ReceiveMetric(Metric metric)
    {
        await backendHandlers.ReceiveMetric(metric);
    }

    public async Task ReceiveWorkerLog(WorkerLogEntry workerLogMessage)
    {
        await backendHandlers.ReceiveWorkerLog(workerLogMessage);
    }


    public async Task ReceiveEngineLog(EngineLogEntry engineLog)
    {
        await backendHandlers.ReceiveEngineLog(engineLog);
    }


    public async Task ReceiveImage(ImageData imageData)
    {
        await backendHandlers.ReceiveImage(imageData);
    }

    public Task ReceiveDeadLetter(string deadLetter)
    {
        Console.WriteLine($"Dead letter received: {deadLetter}");
        return Task.CompletedTask;
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