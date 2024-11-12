using Common.DTOs;
using Common.DTOs.Events;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

namespace StreamHub.Hubs;

public class EngineHub(
    IEngineManager engineManager,
    CancellationService cancellationService,
    IHubContext<EngineHub> hubContext,
    FrontendHandlers frontendHandlers,
    BackendHandlers backendHandlers,
    ILogger<EngineHub> logger)
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
    public async Task<bool> RegisterEngineConnection(BaseEngineInfo baseEngineInfo)
    {
        return await backendHandlers.RegisterEngineConnection(baseEngineInfo, Context);
    }

    public async Task SynchronizeWorkers(List<WorkerChangeEvent> workers, Guid engineId)
    {
        await backendHandlers.SynchronizeWorkers(workers, engineId);
    }


    public async Task ReceiveEngineSystemInfo(EngineSystemInfoModel systemInfo)
    {
        await backendHandlers.ReceiveEngineSystemInfo(systemInfo);
    }


    public async Task ReceiveEngineEvent(EngineChangeEvent engineChangeEvent)
    {
        await backendHandlers.ReceiveEngineEvent(engineChangeEvent);
    }


    public async Task ReceiveWorkerEvent(WorkerChangeEvent workerBaseChangeEvent)
    {
        await backendHandlers.ReceiveWorkerEvent(workerBaseChangeEvent);
    }


    public async Task ReceiveMetric(EngineMetric engineMetric)
    {
        await backendHandlers.ReceiveMetric(engineMetric);
    }

    public async Task ReceiveWorkerLog(WorkerLogEntry workerLogMessage)
    {
        await backendHandlers.ReceiveWorkerLog(workerLogMessage);
    }


    public async Task ReceiveEngineLog(EngineLogEntry engineLog)
    {
        await backendHandlers.ReceiveEngineLog(engineLog);
    }


    public async Task ReceiveImage(WorkerImageData workerImageData)
    {
        await backendHandlers.ReceiveImage(workerImageData);
    }

    public Task ReceiveDeadLetter(string deadLetter)
    {
        logger.LogWarning($"Dead letter received: {deadLetter}");
        return Task.CompletedTask;
    }


    public override async Task OnConnectedAsync()
    {
        var clientType = Context.GetHttpContext()?.Request.Query["clientType"].ToString();
        switch (clientType)
        {
            case "backend":
                logger.LogInformation($"Backend client attempting to connect: {Context.ConnectionId}");
                break;
            case "frontend":
                await Groups.AddToGroupAsync(Context.ConnectionId, "frontendClients");
                logger.LogInformation($"Frontend client connected: {Context.ConnectionId}");
                break;
            default:
                logger.LogWarning($"Unknown client connected: {Context.ConnectionId}");
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
            logger.LogInformation($"Engine disconnected: {Context.ConnectionId}");
        }
        else
        {
            logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        }

        return base.OnDisconnectedAsync(exception);
    }
}