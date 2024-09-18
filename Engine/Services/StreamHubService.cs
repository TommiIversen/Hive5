using Microsoft.AspNetCore.SignalR.Client;
using Common.Models;
using Engine.Commands;
using System.Collections.Concurrent;

namespace Engine.Services;

public class StreamHubService
{
    private readonly ConcurrentDictionary<HubConnection, ConcurrentQueue<IMessage>> _hubQueues = new();
    private readonly MessageQueue _messageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CommandDispatcher _commandDispatcher = new();
    public Guid EngineId { get; } = Guid.NewGuid();
    private const int MessageSendTimeout = 5000; // 5 seconds timeout

    // Constructor for injecting MessageQueue and initializing multiple hubs
    public StreamHubService(MessageQueue messageQueue, IEnumerable<string> hubUrls)
    {
        _messageQueue = messageQueue;

        foreach (var url in hubUrls)
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5) })
                .Build();

            hubConnection.On<Guid, Task<CommandResult>>("StopWorker", async workerId =>
            {
                var command = new StopWorkerCommand(workerId);
                Console.WriteLine($"Worker1111 {workerId} STOPTOPTOTPTO");
                var result = await _commandDispatcher.DispatchAsync(command);
                Console.WriteLine($"Worker {workerId} STOPTOPTOTPTO: {result.Success} {result.Message}");

                return result;
            });

            hubConnection.On<Guid, Task<CommandResult>>("StartWorker", async workerId =>
            {
                var command = new StartWorkerCommand(workerId);
                var result = await _commandDispatcher.DispatchAsync(command);
                return result;
            });
            
            hubConnection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine("Reconnected. Sending SendEngineConnectedAsync messages.");
                await SendEngineConnectedAsync(hubConnection);

            };

            hubConnection.Closed += async (error) =>
            {
                Console.WriteLine($"Connection closed: {error?.Message}. Retrying connection...");
                await TryReconnect(hubConnection);
            };

            _hubQueues[hubConnection] = new ConcurrentQueue<IMessage>();
            _ = Task.Run(async () => await ProcessHubQueueAsync(hubConnection, _cancellationTokenSource.Token));
        }
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Attempting to connect to all StreamHubs...");
    
        // Opret en liste af opgaver for at forbinde til alle hubs parallelt
        var reconnectTasks = _hubQueues.Keys.Select(async hubConnection =>
        {
            Console.WriteLine($"Connecting to {hubConnection.ConnectionId}");
            await TryReconnect(hubConnection);
        });

        // Start processing messages from the global queue
        _ = Task.Run(async () => await ProcessMessagesAsync(_cancellationTokenSource.Token));
        
        // Start alle reconnect-opgaver parallelt
        await Task.WhenAll(reconnectTasks);

    }


    private async Task TryReconnect(HubConnection hubConnection)
    {
        while (true)
        {
            try
            {
                await hubConnection.StartAsync();
                Console.WriteLine($"Connected to {hubConnection.ConnectionId}");
                await SendEngineConnectedAsync(hubConnection);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {hubConnection.ConnectionId}: {ex.Message}. Retrying in 5 seconds...");
                await Task.Delay(5000);
            }
        }
    }

    private async Task SendEngineConnectedAsync(HubConnection hubConnection)
    {
        await hubConnection.InvokeAsync("EngineConnected", EngineId);
    }

    // Global processing of messages
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IMessage message = await _messageQueue.DequeueMessageAsync(cancellationToken);
            //Console.WriteLine($"Processing message: {message.GetType().Name}");

            foreach (var hubConnection in _hubQueues.Keys)
            {
                //Console.WriteLine($"Enqueuing message for {hubConnection.ConnectionId}");
                _hubQueues[hubConnection].Enqueue(message); // Enqueue the message for each hub
            }
        }
    }

    // Process the queue for each hub independently
    private async Task ProcessHubQueueAsync(HubConnection hubConnection, CancellationToken cancellationToken)
    {
        var queue = _hubQueues[hubConnection];
        while (!cancellationToken.IsCancellationRequested)
        {
            if (queue.TryDequeue(out var message))
            {
                // Try sending the message if the hub is connected
                if (hubConnection.State == HubConnectionState.Connected)
                {
                    var sendTask = SendMessageAsync(hubConnection, message);
                    if (await Task.WhenAny(sendTask, Task.Delay(MessageSendTimeout)) == sendTask)
                    {
                        //Console.WriteLine($"Message sent to {hubConnection.ConnectionId}");
                    }
                    else
                    {
                        Console.WriteLine($"Sending message to {hubConnection.ConnectionId} timed out.");
                    }
                }
                else
                {
                    Console.WriteLine($"Hub {hubConnection.ConnectionId} is offline, buffering message.");
                    queue.Enqueue(message); // Re-enqueue the message if the hub is still offline
                    await Task.Delay(5000, cancellationToken); // Retry after a delay
                }
            }

            //await Task.Delay(100, cancellationToken); // Small delay to prevent tight loop
        }
    }

    private async Task SendMessageAsync(HubConnection hubConnection, IMessage message)
    {
        switch (message)
        {
            case Metric metric:
                metric.EngineId = EngineId;
                await SendMetricAsync(hubConnection, metric);
                break;

            case LogEntry log:
                log.EngineId = EngineId;
                await SendLogAsync(hubConnection, log);
                break;

            case ImageData image:
                image.EngineId = EngineId;
                await SendImageAsync(hubConnection, image);
                break;
        }
    }

    // Send metrics via SignalR
    private async Task SendMetricAsync(HubConnection hubConnection, Metric metric)
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("ReceiveMetric", metric);
        }
    }

    // Send logs via SignalR
    private async Task SendLogAsync(HubConnection hubConnection, LogEntry log)
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("ReceiveLog", log);
        }
    }

    // Send image data via SignalR
    private async Task SendImageAsync(HubConnection hubConnection, ImageData image)
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("ReceiveImage", image);
        }
    }
}
