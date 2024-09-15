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

    public StreamHubService()
    {
        Console.WriteLine("Connecting to StreamHub...");
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://127.0.0.1:9000/streamhub")
            .WithAutomaticReconnect()
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
            await SendBufferedMessagesAsync();
        };
    }

    public async Task StartAsync()
    {
        await _hubConnection.StartAsync();
        await SendEngineConnectedAsync();

        // Start metrics timer
        _metricsTimer = new Timer(SendMetrics, null, 100, 1000);
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
            await _hubConnection.InvokeAsync("ReceiveMetric", metric);
        }
        else
        {
            BufferMessage(metric);
        }
    }

    public async Task SendLogAsync(LogEntry log)
    {
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
