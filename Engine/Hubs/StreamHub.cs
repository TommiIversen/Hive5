using System.Collections.Concurrent;
using Common.Models;
using Engine.Commands;
using Engine.Services;
using Microsoft.AspNetCore.Http.Connections;
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
                    .WithUrl($"{url}?clientType=backend", options =>
                    {
                        options.Transports = HttpTransportType.WebSockets; // Kun WebSockets
                    })
                    .WithAutomaticReconnect(new[]
                    {
                        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5)
                    })
                    .Build();

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
                    logger.LogInformation("hubConnection:: Reconnected to {ConnectionId}", hubConnection.ConnectionId);
                    await SendEngineConnectedAsync(hubConnection, url);
                };

                hubConnection.Closed += async (error) =>
                {
                    logger.LogWarning("Connection closed: {Error}", error?.Message);
                    await TryReconnect(hubConnection, url, _cancellationTokenSource.Token);
                };

                _hubQueues[hubConnection] = new MultiQueue(loggerFactory.CreateLogger<MultiQueue>(), maxQueueSize);
                _ = Task.Run(async () => await TryReconnect(hubConnection, url, _cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to {Url}", url);
            }
        }
        _ = Task.Run(async () => await ProcessMessagesAsync(_cancellationTokenSource.Token));
    }

    private async Task TryReconnect(HubConnection hubConnection, string url, CancellationToken token)
    {
        while (true)
        {
            try
            {
                await hubConnection.StartAsync(token);
                _logger.LogInformation("TryReconnect. Connected to {Url} {ConnectionId}", url, hubConnection.ConnectionId);
                await SendEngineConnectedAsync(hubConnection, url);
                break;
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                _logger.LogWarning("TryReconnect: Failed to connect to... {url} Retrying in 5 seconds...", url);
                await Task.Delay(5000, token);
            }
        }
    }

    private async Task SendEngineConnectedAsync(HubConnection hubConnection, string url)
    {
        Console.WriteLine($"------Sending engine Init messages to {EngineId}");
        var engineModel = new EngineBaseInfo
        {
            EngineId = EngineId
        };
        await hubConnection.InvokeAsync("EngineConnected", engineModel);
        var workers = _workerManager.GetAllWorkers(EngineId);
        await hubConnection.InvokeAsync("ReportWorkers", workers);
        await ProcessHubQueueAsync(hubConnection, url, _cancellationTokenSource.Token);
    }

    // Global processing of messages from main queue to per-connection queue
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start ProcessMessagesAsync ------Processing messages to {EngineId}");
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
        Console.WriteLine($"Start ProcessHubQueueAsync ------Processing hub queue to {EngineId}");
        var queue = _hubQueues[hubConnection];
        var sequenceNumber = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            BaseMessage baseMessage = await queue.DequeueMessageAsync(cancellationToken);

            if (hubConnection.State == HubConnectionState.Connected)
            {
                baseMessage.EngineId = EngineId;
                baseMessage.SequenceNumber = sequenceNumber++;
                await SendMessageAsync(hubConnection, baseMessage);
            }
            else
            {
                _logger.LogWarning("Hub {url} is offline, stopping ProcessHubQueueAsync", url);
                break;
            }
        }
    }

    private async Task SendMessageAsync(HubConnection hubConnection, BaseMessage? baseMessage)
    {
        switch (baseMessage)
        {
            case Metric metric:
                await hubConnection.InvokeAsync("ReceiveMetric", metric);
                break;

            case LogEntry log:
                await hubConnection.InvokeAsync("ReceiveLog", log);
                break;

            case ImageData image:
                await hubConnection.InvokeAsync("ReceiveImage", image);
                break;
        }
    }

}