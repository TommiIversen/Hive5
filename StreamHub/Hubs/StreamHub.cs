using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Common.Models;
using StreamHub.Models;

namespace StreamHub.Hubs;

public class StreamHub : Hub
{
    private static readonly ConcurrentDictionary<Guid, EngineInfo> Engines = new();
    private readonly CancellationToken _cancellationTokenSource = new();


    public async Task EngineConnected(Guid engineId)
    {
        Console.WriteLine($"Engine connected: {engineId}");
        var engineInfo = Engines.GetOrAdd(engineId, id => new EngineInfo { EngineId = id });
        engineInfo.ConnectionId = Context.ConnectionId;
        await Clients.Caller.SendAsync("EngineAcknowledged");
    }

    public async Task ReceiveMetric(Metric metric)
    {
        //Console.WriteLine($"Received metric {metric.CPUUsage} {metric.MemoryUsage} from {metric.EngineId}");
        if (Engines.TryGetValue(metric.EngineId, out var engine))
        {
            //Console.WriteLine($"Updating metric for engine {engine.EngineId}");
            engine.LastMetric = metric;
            await Clients.All.SendAsync("UpdateMetric", metric);
        }
        else
        {
            Console.WriteLine($"ReceiveMetric: Engine {metric.EngineId} not found");
        }
    }

    public async Task ReceiveLog(LogEntry log)
    {
        //Console.WriteLine($"Received log from {log.EngineId}");
        await Clients.All.SendAsync("ReceiveLog", log);
    }

    public async Task ReceiveImage(ImageData imageData)
    {
        //Console.WriteLine($"Received image from {imageData.EngineId}");
        await Clients.All.SendAsync("ReceiveImage", imageData);
    }

    public async Task<CommandResult> StopWorker(Guid engineId, Guid workerId)
    {
        if (Engines.TryGetValue(engineId, out var engine))
        {
            try
            {
                // Send "StopWorker" besked til den underliggende SignalR-forbindelse
                Console.WriteLine($"Forwarding StopWorker request for worker {workerId} on engine {engineId}");
            
                var result = await Clients.Client(engine.ConnectionId)
                    .InvokeAsync<CommandResult>("StopWorker", workerId, CancellationToken.None);


                Console.WriteLine($"Received result: {result.Message}");

                // Returner resultatet fra den underliggende worker
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping worker {workerId}: {ex.Message}");
                return new CommandResult(false, $"Failed to stop worker {workerId}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"StopWorker: Engine {engineId} not found");

            // Returnér en fejl til klienten, hvis engine ikke findes
            return new CommandResult(false, $"Engine {engineId} not found.");
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