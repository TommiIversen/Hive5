using Common.DTOs;
using Microsoft.AspNetCore.SignalR;
using StreamHub.Services;

namespace StreamHub.Hubs;

public class FrontendHandlers
{
    private readonly CancellationService _cancellationService;
    private readonly EngineManager _engineManager;
    private readonly IHubContext<EngineHub> _hubContext;

    public FrontendHandlers(EngineManager engineManager, IHubContext<EngineHub> hubContext,
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
        //return new CommandResult(false, "Engine is not connected");
        using var timeoutCts = new CancellationTokenSource(5000);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(_cancellationService.Token, timeoutCts.Token);

        if (existingEngine is {ConnectionInfo.ConnectionId: not null})
        {
            CommandResult result;
            result = await _hubContext.Clients.Client(existingEngine.ConnectionInfo.ConnectionId)
                .InvokeAsync<CommandResult>("RemoveHubConnection", hubUrl, linkedCts.Token);
            Console.WriteLine($"RemoveHubUrlRemoveHubUrlRemoveHubUrlRemoveHubUrl Result: {result.Message}");
        }

        await _hubContext.Clients.Group("frontendClients").SendAsync("EngineChange", _cancellationService.Token);
    }

    public async Task SubscribeToWorkerLogs(string workerId, string engineId, string connectionId)
    {
        Console.WriteLine($"Subscribing to logs for worker {workerId}");
        await _hubContext.Groups.AddToGroupAsync(connectionId, $"worker-{engineId}-{workerId}");
    }

    public async Task UnsubscribeFromWorkerLogs(string workerId, string engineId, string connectionId)
    {
        Console.WriteLine($"Unsubscribing from logs for worker {workerId}");
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"worker-{engineId}-{workerId}");
    }

    public async Task SubscribeToEngineLogs(Guid engineId, string connectionId)
    {
        Console.WriteLine($"Subscribing to logs for engine {engineId}");
        await _hubContext.Groups.AddToGroupAsync(connectionId, $"enginelog-{engineId}");
    }

    public async Task UnsubscribeFromEngineLogs(Guid engineId, string connectionId)
    {
        Console.WriteLine($"Unsubscribing to logs for engine {engineId}");
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"enginelog-{engineId}");
    }
}