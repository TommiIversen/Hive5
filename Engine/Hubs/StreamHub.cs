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
    private const bool DebugFlag = false;
    private readonly WorkerManager _workerManager;


    public StreamHub(MessageQueue messageQueue, ILogger<StreamHub> logger, ILoggerFactory loggerFactory,
        IEnumerable<string> hubUrls, int maxQueueSize, WorkerManager workerManager)
    {
        _logger = logger;
        _messageQueue = messageQueue;
        _loggerFactory = loggerFactory;
        _messageQueue = messageQueue;
        _workerManager = workerManager;
        
        foreach (var url in hubUrls)
        {
            try
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{url}?clientType=backend")
                    .WithAutomaticReconnect(new[]
                    {
                        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5)
                    })
                    .Build();

                hubConnection.On("ReceiveLog", () => { logger.LogInformation("TODO:::: Received log message"); });

                // Handle StopWorker command asynchronously
                hubConnection.On("StopWorker", async (Guid workerId) =>
                {
                    try
                    {
                        var command = new StopWorkerCommand(workerId);
                        logger.LogInformation("Stopping worker {WorkerId}", workerId);
                        var result = await _commandDispatcher.DispatchAsync(command);
                        logger.LogInformation("Worker {WorkerId} stopped: {Success} {Message}", workerId,
                            result.Success, result.Message);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error stopping worker {WorkerId}", workerId);
                        return new CommandResult(false, $"Error: {ex.Message}");
                    }
                });


                hubConnection.On("StartWorker", async (Guid workerId) =>
                {
                    var command = new StartWorkerCommand(workerId);
                    var result = await _commandDispatcher.DispatchAsync(command);
                    return result;
                });

                hubConnection.Reconnected += async (_) =>
                {
                    logger.LogInformation("Reconnected to {ConnectionId}", hubConnection.ConnectionId);
                    await SendEngineConnectedAsync(hubConnection);
                };

                hubConnection.Closed += async (error) =>
                {
                    logger.LogWarning("Connection closed: {Error}", error?.Message);
                    await TryReconnect(hubConnection, url, _cancellationTokenSource.Token);
                };

                _hubQueues[hubConnection] = new MultiQueue(loggerFactory.CreateLogger<MultiQueue>(), maxQueueSize);

                _ = Task.Run(async () => await TryReconnect(hubConnection, url, _cancellationTokenSource.Token));

                _ = Task.Run(async () =>
                    await ProcessHubQueueAsync(hubConnection, url, _cancellationTokenSource.Token));

                _ = Task.Run(async () => await ProcessMessagesAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to {Url}", url);
            }
        }
    }

    private async Task TryReconnect(HubConnection hubConnection, string url, CancellationToken token)
    {
        while (true)
        {
            try
            {
                await hubConnection.StartAsync(token);
                _logger.LogInformation("Connected to {Url} {ConnectionId}", url, hubConnection.ConnectionId);
                await SendEngineConnectedAsync(hubConnection);
                break;
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                _logger.LogWarning("Failed to connect to... {url} Retrying in 5 seconds...", url);
                await Task.Delay(5000, token);
            }
        }
    }

    private async Task SendEngineConnectedAsync(HubConnection hubConnection)
    {
        Console.WriteLine($"------Sending engine connected to {EngineId}");
        var engineModel = new EngineBaseInfo
        {
            EngineId = EngineId
        };
        await hubConnection.InvokeAsync("EngineConnected", engineModel);
        Console.WriteLine($"------###################");
        var workers = _workerManager.GetAllWorkers(EngineId);
        Console.WriteLine("kkkkk");
        Console.WriteLine($"-------List workers count: {workers.Count}");
        await hubConnection.InvokeAsync("ReportWorkers", workers);
    }

    // Global processing of messages
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            BaseMessage? baseMessage = await _messageQueue.DequeueMessageAsync(cancellationToken);
            if (baseMessage == null) continue;

            foreach (var hubConnection in _hubQueues.Keys)
            {
                if (baseMessage is ImageData imageMessage)
                {
                    _hubQueues[hubConnection].EnqueueMessage(imageMessage, $"IMAGE-{imageMessage.WorkerId.ToString()}");
                    continue;
                }

                _hubQueues[hubConnection].EnqueueMessage(baseMessage);

                if (!DebugFlag) continue;
                var queueReport = _hubQueues[hubConnection].ReportQueueContents();
                foreach (var entry in queueReport)
                {
                    _logger.LogDebug("{Key}: {Value}", entry.Key, entry.Value);
                }
            }
        }
    }

    // Process the queue for each hub independently
    private async Task ProcessHubQueueAsync(HubConnection hubConnection, string url,
        CancellationToken cancellationToken)
    {
        var queue = _hubQueues[hubConnection];

        while (!cancellationToken.IsCancellationRequested)
        {
            BaseMessage? baseMessage = await queue.DequeueMessageAsync(cancellationToken);
            if (hubConnection.State == HubConnectionState.Connected)
            {
                var sendTask = SendMessageAsync(hubConnection, baseMessage);
                if (await Task.WhenAny(sendTask, Task.Delay(MessageSendTimeout, cancellationToken)) != sendTask)
                {
                    _logger.LogWarning("Sending message to {url} timed out", url);
                }
            }
            else
            {
                _logger.LogWarning("Hub {url} is offline, buffering message", url);
                await Task.Delay(5000, cancellationToken); // Retry after a delay
            }
        }
    }

    private async Task SendMessageAsync(HubConnection hubConnection, BaseMessage? baseMessage)
    {
        switch (baseMessage)
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