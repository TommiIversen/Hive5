using System.Collections.Concurrent;
using Common.Models;
using Engine.Commands;
using Engine.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace Engine.Hubs;

public class StreamHub
{
    private readonly ConcurrentDictionary<HubConnection, MultiQueue> _hubQueues = new();
    private readonly MessageQueue _messageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ILoggerFactory _loggerFactory;

    private readonly CommandDispatcher _commandDispatcher = new();
    public Guid EngineId { get; } = Guid.NewGuid();
    private const int MessageSendTimeout = 5000; // 5 seconds timeout

    private readonly ILogger<StreamHub> _logger;

    public StreamHub(MessageQueue messageQueue, ILogger<StreamHub> logger, ILoggerFactory loggerFactory, IEnumerable<string> hubUrls, int maxQueueSize)
    {
        _messageQueue = messageQueue;
        _logger = logger;
        _loggerFactory = loggerFactory; // Save the injected loggerFactory

        _messageQueue = messageQueue;

        foreach (var url in hubUrls)
        {
            try
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect(new[]
                    {
                        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5)
                    })
                    .Build();

                hubConnection.On("ReceiveLog", (string data) =>
                {
                    Console.WriteLine("JFDGJDKFGJDKFGJKDGKDJGDGJDGLKDJ");
                });
                
                // Handle StopWorker command asynchronously
                hubConnection.On("StopWorker", async (Guid workerId) =>
                {
                    try
                    {
                        var command = new StopWorkerCommand(workerId);
                        Console.WriteLine($"Stopping worker {workerId}");

                        var result = await _commandDispatcher.DispatchAsync(command);
                        Console.WriteLine($"Worker {workerId} stopped: {result.Success} {result.Message}");

                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error stopping worker {workerId}: {ex.Message}");
                        return new CommandResult(false, $"Error: {ex.Message}");
                    }
                });


                hubConnection.On("StartWorker", async (Guid workerId) =>
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
                    await TryReconnect(hubConnection, _cancellationTokenSource.Token);
                };

                _hubQueues[hubConnection] = new MultiQueue(loggerFactory.CreateLogger<MultiQueue>(), maxQueueSize);

                _ = Task.Run(async () => await ProcessHubQueueAsync(hubConnection, _cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StreamHub Exception: Failed to connect to {url}: {ex.Message}");
            }
        }
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Attempting to connect to all StreamHubs...");
        var reconnectTasks = _hubQueues.Keys.Select(async hubConnection =>
        {
            Console.WriteLine($"Connecting to {hubConnection.ConnectionId}");
            await TryReconnect(hubConnection, _cancellationTokenSource.Token);
        });

        _ = Task.Run(async () => await ProcessMessagesAsync(_cancellationTokenSource.Token));
        await Task.WhenAll(reconnectTasks);
    }

    private async Task TryReconnect(HubConnection hubConnection, CancellationToken token)
    {
        while (true)
        {
            try
            {
                await hubConnection.StartAsync(token);
                Console.WriteLine($"Connected to {hubConnection.ConnectionId}");
                await SendEngineConnectedAsync(hubConnection);
                break;
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to connect to {hubConnection.ConnectionId}: {ex.Message}. Retrying in 5 seconds...");
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
            foreach (var hubConnection in _hubQueues.Keys)
            {
                if (message is ImageData imageMessage)
                {
                    _hubQueues[hubConnection].EnqueueMessage(imageMessage, $"IMEGE-{imageMessage.WorkerId.ToString()}");
                    continue;
                }
                _hubQueues[hubConnection].EnqueueMessage(message);
                var queueReport = _hubQueues[hubConnection].ReportQueueContents();
                foreach (var entry in queueReport)
                {
                    Console.WriteLine($"{entry.Key}: {entry.Value}");
                }
            }
        }
    }

    // Process the queue for each hub independently
    private async Task ProcessHubQueueAsync(HubConnection hubConnection, CancellationToken cancellationToken)
    {
        var queue = _hubQueues[hubConnection];

        while (!cancellationToken.IsCancellationRequested)
        {
            IMessage message = await queue.DequeueMessageAsync(cancellationToken);
            if (hubConnection.State == HubConnectionState.Connected)
            {
                var sendTask = SendMessageAsync(hubConnection, message);
                if (await Task.WhenAny(sendTask, Task.Delay(MessageSendTimeout)) != sendTask)
                {
                    Console.WriteLine($"Sending message to {hubConnection.ConnectionId} timed out.");
                }
            }
            else
            {
                Console.WriteLine($"Hub {hubConnection.ConnectionId} is offline, buffering message.");
                //queue.EnqueueMessage(message); // Re-enqueue the message if the hub is still offline
                await Task.Delay(5000, cancellationToken); // Retry after a delay
            }
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

    private async Task SendMetricAsync(HubConnection hubConnection, Metric metric)
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("ReceiveMetric", metric);
        }
    }

    private async Task SendLogAsync(HubConnection hubConnection, LogEntry log)
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("ReceiveLog", log);
        }
    }

    private async Task SendImageAsync(HubConnection hubConnection, ImageData image)
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("ReceiveImage", image);
        }
    }
}