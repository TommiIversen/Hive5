using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using Engine.Models;
using StreamHub.Models;

namespace Engine.Services;

public class StreamHubService
{
    private readonly HubConnection _hubConnection;
    public Guid EngineId { get; } = Guid.NewGuid();
    private readonly Dictionary<Guid, Worker> _workers = new();
    private readonly ConcurrentQueue<object> _messageBuffer = new();
    private const int BufferSize = 200;
    private Timer _metricsTimer;
    
    public IReadOnlyDictionary<Guid, Worker> Workers => _workers; // Expose workers for UI


    public StreamHubService()
    {
        Console.WriteLine("Setting up connection to StreamHub...");
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://127.0.0.1:9000/streamhub")
            .WithAutomaticReconnect() // Bruger SignalR's indbyggede auto-genforbindelse
            .Build();

        // Håndter indkommende kommandoer
        _hubConnection.On<Guid>("StopWorker", workerId =>
        {
            if (_workers.TryGetValue(workerId, out var worker))
            {
                worker.Stop();
            }
        });

        // Send bufferede beskeder ved genforbindelse
        _hubConnection.Reconnected += async (connectionId) =>
        {
            Console.WriteLine("Reconnected. Sending buffered messages.");
            await SendBufferedMessagesAsync();
        };

        _hubConnection.Closed += async (error) =>
        {
            Console.WriteLine($"Connection closed: {error?.Message}. Retrying connection...");
            await TryReconnect();
        };
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Attempting to connect to StreamHub...");
        await TryReconnect();
        
        // Start metrics timer
        _metricsTimer = new Timer(SendMetrics, null, 0, 10000); // 10 sek interval
    }
    
    private async Task TryReconnect()
    {
        while (true) // Uendelig loop for genforbindelse
        {
            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("Connected to StreamHub.");
                await SendEngineConnectedAsync();
                break; // Bryder ud af loopet, når forbindelsen lykkes
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to StreamHub: {ex.Message}. Retrying in 5 seconds...");
                await Task.Delay(5000); // Vent 5 sekunder før nyt forsøg
            }
        }
    }

    private async void SendMetrics(object state)
    {
        var metric = new Metric
        {
            EngineId = EngineId,
            Timestamp = DateTime.UtcNow,
            CPUUsage = GetFakeCPUUsage(),
            MemoryUsage = GetFakeMemoryUsage()
        };
        Console.WriteLine($"Sending metric: {metric.CPUUsage}% CPU, {metric.MemoryUsage} MB memory");
        await SendMetricAsync(metric);
    }

    private double GetFakeCPUUsage()
    {
        return Random.Shared.NextDouble() * 100;
    }

    private double GetFakeMemoryUsage()
    {
        return Random.Shared.NextDouble() * 16000; // MB
    }

    private async Task SendEngineConnectedAsync()
    {
        await _hubConnection.InvokeAsync("EngineConnected", EngineId);
    }

    public async Task SendMetricAsync(Metric metric)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("ReceiveMetric", metric);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending metric: {ex.Message}");
                BufferMessage(metric); // Buffer besked ved fejl
            }
        }
        else
        {
            BufferMessage(metric); // Buffer besked, hvis ikke forbundet
        }
    }

    public async Task SendLogAsync(LogEntry log)
    {
        log.EngineId = EngineId; // Centraliser EngineId-håndteringen
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("ReceiveLog", log);
        }
        else
        {
            BufferMessage(log);
        }
    }

    public async Task SendImageAsync(ImageData imageData)
    {
        imageData.EngineId = EngineId; // Centraliser EngineId-håndteringen
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("ReceiveImage", imageData);
        }
        else
        {
            BufferMessage(imageData);
        }
    }

    private void BufferMessage(object message)
    {
        if (_messageBuffer.Count >= BufferSize)
        {
            _messageBuffer.TryDequeue(out _);
        }
        _messageBuffer.Enqueue(message);
    }

    private async Task SendBufferedMessagesAsync()
    {
        while (_messageBuffer.TryDequeue(out var message))
        {
            switch (message)
            {
                case Metric metric:
                    await SendMetricAsync(metric);
                    break;
                case LogEntry log:
                    await SendLogAsync(log);
                    break;
                case ImageData image:
                    await SendImageAsync(image);
                    break;
            }
        }
    }

    public void AddWorker(Worker worker)
    {
        _workers[worker.WorkerId] = worker;
    }

    public void RemoveWorker(Guid workerId)
    {
        _workers.Remove(workerId);
    }
}
