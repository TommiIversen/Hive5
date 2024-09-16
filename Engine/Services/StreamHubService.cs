using Microsoft.AspNetCore.SignalR.Client;
using StreamHub.Models;


namespace Engine.Services;

public class StreamHubService
{
    private readonly HubConnection _hubConnection;
    private readonly MessageQueue _messageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    public Guid EngineId { get; } = Guid.NewGuid();

    // Inject MessageQueue in the constructor
    public StreamHubService(MessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://127.0.0.1:9000/streamhub")
            .WithAutomaticReconnect()
            .Build();

        // Håndter indkommende kommandoer
        _hubConnection.On<Guid>("StopWorker",
            workerId =>
            {
                Console.WriteLine($"Received command to stop worker {workerId}");
            });

        _hubConnection.Reconnected += async (connectionId) =>
        {
            await SendEngineConnectedAsync();
            await SendBufferedMessagesAsync();
        };

        _hubConnection.Closed += async (error) =>
        {
            Console.WriteLine($"Connection closed: {error?.Message}. Retrying...");
            await TryReconnect();
        };
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Attempting to connect to StreamHub...");
        await TryReconnect();

        // Start processing messages from the queue
        _ = Task.Run(async () => await ProcessMessagesAsync(_cancellationTokenSource.Token));
    }

    private async Task TryReconnect()
    {
        while (true)
        {
            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("Connected to StreamHub.");
                await SendEngineConnectedAsync();
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}. Retrying in 5 seconds...");
                await Task.Delay(5000);
            }
        }
    }

    private async Task SendEngineConnectedAsync()
    {
        await _hubConnection.InvokeAsync("EngineConnected", EngineId);
    }

    // Process messages from the queue
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await _messageQueue.DequeueMessageAsync(cancellationToken);
            Console.WriteLine($"Processing message: {message.GetType().Name}");
            // Process the message based on its type
            switch (message)
            {
                case Metric metric:
                    metric.EngineId = EngineId;
                    await SendMetricAsync(metric);
                    break;

                case LogEntry log:
                    log.EngineId = EngineId;
                    await SendLogAsync(log);
                    break;

                case ImageData image:
                    image.EngineId = EngineId;
                    await SendImageAsync(image);
                    break;
            }
        }
    }

    // Send metrics via SignalR
    private async Task SendMetricAsync(Metric metric)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("ReceiveMetric", metric);
        }
    }

    // Send logs via SignalR
    private async Task SendLogAsync(LogEntry log)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("ReceiveLog", log);
        }
        else
        {
            Console.WriteLine("Buffering log message...");
        }
    }

    // Send image data via SignalR
    private async Task SendImageAsync(ImageData image)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("ReceiveImage", image);
        }
    }

    // Buffer beskeder hvis de ikke kan sendes med det samme
    private async Task SendBufferedMessagesAsync()
    {
        // Logic for resending buffered messages after reconnect
    }
}