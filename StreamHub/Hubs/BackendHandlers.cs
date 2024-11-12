using Common.DTOs;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

namespace StreamHub.Hubs;

public class BackendHandlers
{
    private readonly CancellationService _cancellationService;
    private readonly IEngineManager _engineManager;
    private readonly IHubContext<EngineHub> _hubContext;

    public BackendHandlers(IEngineManager engineManager, IHubContext<EngineHub> hubContext,
        CancellationService cancellationService)
    {
        _engineManager = engineManager;
        _hubContext = hubContext;
        _cancellationService = cancellationService;
    }

    public async Task<bool> RegisterEngineConnection(BaseEngineInfo baseEngineInfo, HubCallerContext context)
    {
        // Tjek om engine allerede er forbundet
        if (_engineManager.TryGetEngine(baseEngineInfo.EngineId, out var existingEngine) &&
            existingEngine != null &&
            !string.IsNullOrEmpty(existingEngine.ConnectionInfo.ConnectionId))
        {
            Console.WriteLine(
                $"RegisterEngineConnection Engine {baseEngineInfo.EngineId} is already connected. Rejecting new connection.");
            return false; // Afvis forbindelsen, da engine allerede er aktiv
        }

        Console.WriteLine($"RegisterEngineConnection Engine {baseEngineInfo.EngineId} connected.");

        // Tilføj eller opdater engine og sæt forbindelses ID
        var engine = _engineManager.GetOrAddEngine(baseEngineInfo);
        engine.ConnectionInfo.ConnectionId = context.ConnectionId;
        engine.ConnectionInfo.OnlineSince = DateTime.UtcNow;
        engine.Info = baseEngineInfo;

        // Få flere oplysninger om forbindelsen
        var httpContext = context.GetHttpContext();
        if (httpContext != null)
        {
            engine.ConnectionInfo.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            engine.ConnectionInfo.Port = httpContext.Connection.RemotePort;
            engine.ConnectionInfo.LocalPort = httpContext.Connection.LocalPort;
            engine.ConnectionInfo.TransportType =
                context.Features.Get<IHttpTransportFeature>()?.TransportType.ToString();

            Console.WriteLine(
                $"Connected from IP: {engine.ConnectionInfo.IpAddress}, Port: {engine.ConnectionInfo.Port}, Transport: {engine.ConnectionInfo.TransportType}, Time: {engine.ConnectionInfo.OnlineSince}");
        }

        // Tilføj engine til backendClients-gruppen efter godkendelse
        await _hubContext.Groups.AddToGroupAsync(context.ConnectionId, "backendClients");
        await _hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", _cancellationService.Token);
        return true; // Forbindelsen er godkendt
    }

    public async Task SynchronizeWorkers(List<WorkerChangeEvent> workers, Guid engineId)
    {
        _engineManager.SynchronizeWorkers(workers, engineId);
        await _hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", _cancellationService.Token);
    }

    public async Task ReceiveEngineSystemInfo(EngineSystemInfoModel systemInfo)
    {
        if (_engineManager.TryGetEngine(systemInfo.EngineId, out var engine))
        {
            if (engine != null) engine.SystemInfo = systemInfo;
            await _hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", _cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"SendSystemInfo: Engine {systemInfo.EngineId} not found");
        }
    }

    public async Task ReceiveEngineEvent(EngineChangeEvent engineChangeEvent)
    {
        switch (engineChangeEvent.ChangeEventType)
        {
            // TODO implementer de andre eventtyper
            // case EventType.Deleted:
            //     engineManager.RemoveEngine(engineChangeEvent.EngineId);
            //     await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
            //     break;
            // case EventType.Created:
            //     engineManager.AddOrUpdateEngine(engineChangeEvent);
            //     await hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", cancellationService.Token);
            //     break;
            case ChangeEventType.Updated:
                _engineManager.UpdateBaseInfo(engineChangeEvent);
                await _hubContext.Clients.Group("frontendClients")
                    .SendAsync("EngineChange", _cancellationService.Token);
                break;
        }
    }


    public async Task ReceiveMetric(EngineMetric engineMetric)
    {
        if (_engineManager.TryGetEngine(engineMetric.EngineId, out var engine))
        {
            engine?.AddMetric(engineMetric);

            // first engineMetric ?
            if (engine?.LastMetric == null)
                await _hubContext.Clients.Group("frontendClients")
                    .SendAsync("EngineChange", _cancellationService.Token);

            await _hubContext.Clients.All.SendAsync($"UpdateMetric-{engineMetric.EngineId}", engineMetric,
                _cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveMetric: Engine {engineMetric.EngineId} not found");
        }
    }


    public async Task ReceiveWorkerEvent(WorkerChangeEvent workerBaseChangeEvent)
    {
        Console.WriteLine(
            $"ReceiveWorkerEvent: {workerBaseChangeEvent.ChangeEventType} - {workerBaseChangeEvent.WorkerId} - {workerBaseChangeEvent.State}");
        switch (workerBaseChangeEvent.ChangeEventType)
        {
            case ChangeEventType.Deleted:
                _engineManager.RemoveWorker(workerBaseChangeEvent.EngineId, workerBaseChangeEvent.WorkerId);
                await _hubContext.Clients.Group("frontendClients")
                    .SendAsync("EngineChange", _cancellationService.Token);
                break;
            case ChangeEventType.Created:
                _engineManager.AddOrUpdateWorker(workerBaseChangeEvent);
                await _hubContext.Clients.Group("frontendClients")
                    .SendAsync("EngineChange", _cancellationService.Token);
                break;
            case ChangeEventType.Updated:
                _engineManager.AddOrUpdateWorker(workerBaseChangeEvent);
                var workerEventTopic =
                    $"WorkerChangeEvent-{workerBaseChangeEvent.EngineId}-{workerBaseChangeEvent.WorkerId}";

                await _hubContext.Clients.Group("frontendClients")
                    .SendAsync(workerEventTopic, workerBaseChangeEvent, _cancellationService.Token);
                break;
        }
    }


    public async Task ReceiveWorkerLog(WorkerLogEntry workerLogMessage)
    {
        if (!_engineManager.TryGetEngine(workerLogMessage.EngineId, out var engine))
        {
            Console.WriteLine($"ReceiveLog: Engine {workerLogMessage.EngineId} not found");
            return;
        }

        var wasAdded = engine?.AddWorkerLog(workerLogMessage.WorkerId, workerLogMessage) ?? false;

        if (wasAdded)
        {
            var groupName = $"WorkerLogGroup-{workerLogMessage.EngineId}-{workerLogMessage.WorkerId}";
            var methodName = $"ReceiveWorkerLog-{workerLogMessage.EngineId}-{workerLogMessage.WorkerId}";

            await _hubContext.Clients.Group(groupName).SendAsync(methodName, workerLogMessage);
        }
        else
        {
            Console.WriteLine($"ReceiveLog: Worker {workerLogMessage.WorkerId} not found");
        }
    }

    public async Task ReceiveEngineLog(EngineLogEntry engineLog)
    {
        Console.WriteLine($"ReceiveEngineLog: {engineLog.Message}");
        if (_engineManager.TryGetEngine(engineLog.EngineId, out var engine))
        {
            engine?.AddEngineLog(engineLog);
            await _hubContext.Clients.Group($"enginelog-{engineLog.EngineId}").SendAsync("ReceiveEngineLog", engineLog);
        }
        else
        {
            Console.WriteLine($"ReceiveLog: Engine {engineLog.EngineId} not found");
        }
    }

    public async Task ReceiveImage(WorkerImageData workerImageData)
    {
        var worker = _engineManager.GetWorker(workerImageData.EngineId, workerImageData.WorkerId);

        if (worker != null)
        {
            worker.LastImage = $"data:image/jpeg;base64,{Convert.ToBase64String(workerImageData.ImageBytes)}";
            await _hubContext.Clients.Group("frontendClients")
                .SendAsync($"ReceiveImage-{workerImageData.EngineId}-{workerImageData.WorkerId}", workerImageData,
                    _cancellationService.Token);
        }
        else
        {
            Console.WriteLine($"ReceiveImage: Worker {workerImageData.WorkerId} not found");
        }
    }
}