using Common.DTOs.Commands;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

namespace StreamHub.Hubs;

public class FrontendHandlers
{
    private readonly CancellationService _cancellationService;
    private readonly IEngineManager _engineManager;
    private readonly IHubContext<EngineHub> _hubContext;

    public FrontendHandlers(IEngineManager engineManager, IHubContext<EngineHub> hubContext,
        CancellationService cancellationService)
    {
        _engineManager = engineManager;
        _hubContext = hubContext;
        _cancellationService = cancellationService;
    }

    public async Task RemoveEngine(Guid engineId)
    {
        Console.WriteLine($"Removing engine {engineId}");
        _engineManager.RemoveEngine(engineId);
        await _hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", _cancellationService.Token);
    }


    public async Task RemoveHubUrl(string hubUrl, Guid engineId)
    {
        Console.WriteLine($"Removing hub connection for URL: {hubUrl}");

        // Tjek om engine allerede er forbundet
        if (_engineManager.TryGetEngine(engineId, out var existingEngine) &&
            string.IsNullOrEmpty(existingEngine?.ConnectionInfo.ConnectionId))
            Console.WriteLine("Engine is not connected.");

        using var timeoutCts = new CancellationTokenSource(5000);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(_cancellationService.Token, timeoutCts.Token);

        if (existingEngine is { ConnectionInfo.ConnectionId: not null })
        {
            CommandResult result;
            result = await _hubContext.Clients.Client(existingEngine.ConnectionInfo.ConnectionId)
                .InvokeAsync<CommandResult>("RemoveHubConnection", hubUrl, linkedCts.Token);
        }

        await _hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", _cancellationService.Token);
    }

    public async Task SubscribeToWorkerLogs(string workerId, string engineId, string connectionId)
    {
        Console.WriteLine($"Subscribing to logs for worker {workerId}");
        await _hubContext.Groups.AddToGroupAsync(connectionId, $"WorkerLogGroup-{engineId}-{workerId}");
    }

    public async Task UnsubscribeFromWorkerLogs(string workerId, string engineId, string connectionId)
    {
        Console.WriteLine($"Unsubscribing from logs for worker {workerId}");

        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"WorkerLogGroup-{engineId}-{workerId}");
    }

    public async Task SubscribeToEngineLogs(Guid engineId, string connectionId)
    {
        Console.WriteLine($"Subscribing to logs for engine {engineId}");
        await _hubContext.Groups.AddToGroupAsync(connectionId, $"EngineLogGroup-{engineId}");
    }

    public async Task UnsubscribeFromEngineLogs(Guid engineId, string connectionId)
    {
        Console.WriteLine($"Unsubscribing to logs for engine {engineId}");
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"EngineLogGroup-{engineId}");
    }
}