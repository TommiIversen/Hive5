using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using StreamHub.Models;

namespace StreamHub.Hubs;

public class StreamHub : Hub
{
    private static readonly ConcurrentDictionary<Guid, EngineInfo> Engines = new();

    public async Task EngineConnected(Guid engineId)
    {
        Console.WriteLine($"Engine connected: {engineId}");
        var engineInfo = Engines.GetOrAdd(engineId, id => new EngineInfo { EngineId = id });
        engineInfo.ConnectionId = Context.ConnectionId;
        await Clients.Caller.SendAsync("EngineAcknowledged");
    }

    public async Task ReceiveMetric(Metric metric)
    {
        Console.WriteLine($"Received metric from {metric.EngineId}");
        if (Engines.TryGetValue(metric.EngineId, out var engine))
        {
            engine.LastMetric = metric;
            await Clients.All.SendAsync("UpdateMetric", metric);
        }
    }

    public async Task ReceiveLog(LogEntry log)
    {
        Console.WriteLine($"Received log from {log.EngineId}");
        await Clients.All.SendAsync("ReceiveLog", log);
    }

    public async Task ReceiveImage(ImageData imageData)
    {
        Console.WriteLine($"Received image from {imageData.EngineId}");
        await Clients.All.SendAsync("ReceiveImage", imageData);
    }

    public async Task StopWorker(Guid engineId, Guid workerId)
    {
        if (Engines.TryGetValue(engineId, out var engine))
        {
            await Clients.Client(engine.ConnectionId).SendAsync("StopWorker", workerId);
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var engine = Engines.Values.FirstOrDefault(e => e.ConnectionId == Context.ConnectionId);
        if (engine != null)
        {
            engine.ConnectionId = null;
        }
        return base.OnDisconnectedAsync(exception);
    }
}

public class EngineInfo
{
    public Guid EngineId { get; set; }
    public string ConnectionId { get; set; }
    public Metric LastMetric { get; set; }
}