using System.Collections.Concurrent;
using Common.Models;
using Engine.DAL.Entities;
using Engine.Models;
using Engine.Services;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Engine.Hubs;

public class StreamHub
{
    private readonly ConcurrentDictionary<HubConnection, MultiQueue> _hubConnectionMessageQueue = new();
    private readonly ConcurrentDictionary<HubConnection, DateTime> _hubConnectionSyncTimestamps = new();

    private readonly MessageQueue _globalMessageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly ILogger<StreamHub> _logger;
    private readonly WorkerManager _workerManager;

    private readonly IEngineService _engineService;
    private readonly EngineEntities _engineInfo;
    
    // fild to init date time
    private readonly DateTime _initDateTime = DateTime.UtcNow;

    public StreamHub(
        MessageQueue globalMessageQueue,
        ILogger<StreamHub> logger,
        ILoggerFactory loggerFactory,
        WorkerManager workerManager,
        IEngineService engineService,
        IOptions<StreamHubOptions> options)
    {
        _logger = logger;
        _globalMessageQueue = globalMessageQueue;
        _workerManager = workerManager;
        _engineService = engineService;
        var hubUrls = options.Value.HubUrls;
        var maxQueueSize = options.Value.MaxQueueSize;

        _engineInfo = GetEngineInfo();
        
        foreach (var url in hubUrls)
        {
            try
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{url}?clientType=backend", connectionOptions =>
                    {
                        connectionOptions.Transports = HttpTransportType.WebSockets; // Kun WebSockets
                    })
                    .WithAutomaticReconnect(new[]
                    {
                        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5)
                    })
                    .Build();

                // Handle StopWorker command asynchronously
                hubConnection.On("StopWorker", async (WorkerOperationMessage message) =>
                {
                    logger.LogInformation("hubConnection.On: Got StopWorker: {WorkerId}", message.WorkerId);
                    var commandResult = await _workerManager.StopWorkerAsync(message.WorkerId);
                    return commandResult;
                });

                hubConnection.On("StartWorker", async (WorkerOperationMessage message) =>
                {
                    logger.LogInformation("hubConnection.On: Got StartWorker: {WorkerId}", message.WorkerId);
                    var commandResult = await _workerManager.StartWorkerAsync(message.WorkerId);
                    return commandResult;
                });
                
                // Handle RemoveWorker command asynchronously
                hubConnection.On("RemoveWorker", async (WorkerOperationMessage message) =>
                {
                    logger.LogInformation("hubConnection.On: Got RemoveWorker: {WorkerId}", message.WorkerId);
                    var commandResult = await _workerManager.RemoveWorkerAsync(message.WorkerId);
                    return commandResult;
                });
                
                // Handle ResetWatchdogEventCount command asynchronously
                hubConnection.On("ResetWatchdogEventCount", async (WorkerOperationMessage message) =>
                {
                    logger.LogInformation("hubConnection.On: Got ResetWatchdogEventCount: {WorkerId}", message.WorkerId);
                    var commandResult = await _workerManager.ResetWatchdogEventCountAsync(message.WorkerId);
                    logger.LogInformation("Reset Watchdog Event Count Result for worker {WorkerId}: {Message}", message.WorkerId, commandResult.Message);
                    return commandResult;
                });
                
                hubConnection.On("EnableDisableWorker", async (WorkerEnableDisableMessage message) =>
                {
                    logger.LogInformation("hubConnection.On: Got EnableDisableWorker: {WorkerId}, Enable: {Enable}", message.WorkerId, message.Enable);
                    var commandResult = await _workerManager.EnableDisableWorkerAsync(message.WorkerId, message.Enable);
                    return commandResult;
                });
                
                // Add new SignalR handler for creating workers
                hubConnection.On("CreateWorker", async (WorkerCreate workerCreate) =>
                {
                    logger.LogInformation("hubConnection.On: Got CreateWorker: {WorkerName}", workerCreate.Name);

                    // Opret og tilføj worker asynkront via WorkerManager
                    var workerService = await _workerManager.AddWorkerAsync(_engineInfo.EngineId, workerCreate);

                    // Hvis workerService er null, findes arbejderen allerede
                    if (workerService == null)
                    {
                        logger.LogWarning("Worker with ID {WorkerId} already exists.", workerCreate.WorkerId);
                        return new CommandResult(false, $"Worker with ID {workerCreate.WorkerId} already exists.");
                    }

                    // Start arbejderen, hvis den blev tilføjet korrekt
                    var result = await _workerManager.StartWorkerAsync(workerService.WorkerId);

                    return result;
                });

                
                hubConnection.Reconnected += async (_) =>
                {
                    logger.LogInformation("hubConnection:: Reconnected to streamhub {url} - {ConnectionId}", url,
                        hubConnection.ConnectionId);
                    await SendEngineConnectedAsync(hubConnection, url);
                };

                hubConnection.Closed += async (error) =>
                {
                    logger.LogWarning("Connection closed: {Error}", error?.Message);
                    await Task.Delay(5000); // Vent før næste forsøg
                    await TryReconnect(hubConnection, url, _cancellationTokenSource.Token);
                };

                _hubConnectionMessageQueue[hubConnection] =
                    new MultiQueue(loggerFactory.CreateLogger<MultiQueue>(), maxQueueSize);
                _ = Task.Run(async () => await TryReconnect(hubConnection, url, _cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to {Url}", url);
            }
        }

        _ = Task.Run(async () => await RouteMessagesToClientQueuesAsync(_cancellationTokenSource.Token));
    }
    
    private EngineEntities GetEngineInfo()
    {
        try
        {
            return _engineService.GetEngineAsync().Result ?? throw new InvalidOperationException(); // Blokerer synkront indtil engine-info er hentet
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve engine info.");
            throw; // Hvis det fejler, stop start af applikationen
        }
    }

    private async Task TryReconnect(HubConnection hubConnection, string url, CancellationToken token)
    {
        while (true)
        {
            try
            {
                await hubConnection.StartAsync(token);
                _logger.LogInformation("TryReconnect. Connected to {Url} {ConnectionId}", url,
                    hubConnection.ConnectionId);
                await SendEngineConnectedAsync(hubConnection, url);
                break; // Stop genforbindelses-loopet, når forbindelsen er oprettet
            }
            catch when (token.IsCancellationRequested)
            {
                break; // Stop, hvis token er annulleret
            }
            catch (Exception)
            {
                _logger.LogWarning("TryReconnect: Failed to connect to {url}. Retrying in 5 seconds...", url);
                await Task.Delay(5000, token); // Vent før næste forsøg
            }
        }
    }


    private async Task SendEngineConnectedAsync(HubConnection hubConnection, string streamhubUrl)
    {
        var syncTimestamp = DateTime.UtcNow;
        _hubConnectionSyncTimestamps[hubConnection] = syncTimestamp;

        Console.WriteLine($"----------Sending engine Init messages to streamHub on: {streamhubUrl}");

        var engineModel = new EngineBaseInfo
        {
            EngineId = _engineInfo.EngineId,
            EngineName = _engineInfo.Name,
            EngineDescription = _engineInfo.Description,
            Version = _engineInfo.Version,
            InstallDate = _engineInfo.InstallDate,
            EngineStartDate = _initDateTime
        };

        // Brug bool resultatet for at afgøre, hvad der skal ske
        bool connectionResult = await hubConnection.InvokeAsync<bool>("RegisterEngineConnection", engineModel);

        if (connectionResult)
        {
            Console.WriteLine("Connection to StreamHub acknowledged, synchronizing workers...");
            var workers = await _workerManager.GetAllWorkers(_engineInfo.EngineId);
            await hubConnection.InvokeAsync("SynchronizeWorkers", workers, _engineInfo.EngineId);
            await ProcessClientMessagesAsync(hubConnection, streamhubUrl, _cancellationTokenSource.Token);
        }
        else
        {
            Console.WriteLine("Connection to StreamHub rejected.");
            // Eventuelt stoppe forbindelsen eller tage anden handling
            await hubConnection.StopAsync();
        }
    }

    // Global processing of messages from main queue to per-connection queue
    private async Task RouteMessagesToClientQueuesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start RouteMessagesToClientQueuesAsync ------Processing global queue to clients");
        while (!cancellationToken.IsCancellationRequested)
        {
            BaseMessage baseMessage = await _globalMessageQueue.DequeueMessageAsync(cancellationToken);
            foreach (var hubConnection in _hubConnectionMessageQueue.Keys)
            {
                if (baseMessage is ImageData imageMessage)
                {
                    _hubConnectionMessageQueue[hubConnection]
                        .EnqueueMessage(imageMessage, $"IMAGE-{imageMessage.WorkerId}");
                    continue;
                }

                _hubConnectionMessageQueue[hubConnection].EnqueueMessage(baseMessage);
            }
        }
    }

    // Process the queue for each streamhub signalR connection independently
    private async Task ProcessClientMessagesAsync(HubConnection hubConnection, string url,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start ProcessClientMessagesAsync ------Processing hub queue to {_engineInfo.EngineId}");
        var queue = _hubConnectionMessageQueue[hubConnection];
        var sequenceNumber = 0;

        // Slå synkroniseringstidspunktet op
        if (!_hubConnectionSyncTimestamps.TryGetValue(hubConnection, out var syncTimestamp))
        {
            syncTimestamp = DateTime.MinValue; // Hvis ingen timestamp er fundet, lad den være ældre end alle beskeder
        }
        
        while (!cancellationToken.IsCancellationRequested)
        {
            BaseMessage baseMessage = await queue.DequeueMessageAsync(cancellationToken);
            
            // Message Filter: Filtrer forældede WorkerEvent-beskeder
            if (baseMessage is WorkerEvent workerEvent && workerEvent.Timestamp < syncTimestamp)
            {
                Console.WriteLine($"✂Skipping outdated event for streamHub: {url} - {workerEvent.EventType} {workerEvent.Name}");
                continue;
            }
            
            if (hubConnection.State == HubConnectionState.Connected)
            {
                EnrichMessage(baseMessage, sequenceNumber++);
                await MessageRouter.RouteMessageToClientAsync(hubConnection, baseMessage);
            }
            else
            {
                _logger.LogWarning("Hub {url} is offline, stopping ProcessClientMessagesAsync", url);
                break;
            }
        }
    }

    private void EnrichMessage(BaseMessage baseMessage, int sequenceNumber)
    {
        baseMessage.EngineId = _engineInfo.EngineId;
        baseMessage.SequenceNumber = sequenceNumber;
    }
}