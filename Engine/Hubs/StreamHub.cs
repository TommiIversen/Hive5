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

    //private Guid EngineId { get; } = Guid.NewGuid();
    private readonly ILogger<StreamHub> _logger;
    private readonly WorkerManager _workerManager;

    private readonly IEngineService _engineService;
    private EngineEntities _engineInfo;

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
                hubConnection.On("StopWorker", async (string workerId) =>
                {
                    logger.LogInformation("hubConnection.On: Got StopWorker: {WorkerId}", workerId);
                    var commandResult = await _workerManager.StopWorkerAsync(workerId);
                    return commandResult;
                });

                hubConnection.On("StartWorker", async (string workerId) =>
                {
                    logger.LogInformation("hubConnection.On: Got StartWorker: {WorkerId}", workerId);
                    var commandResult = await _workerManager.StartWorkerAsync(workerId);
                    return commandResult;
                });
                
                // Handle RemoveWorker command asynchronously
                hubConnection.On("RemoveWorker", async (string workerId) =>
                {
                    logger.LogInformation("hubConnection.On: Got RemoveWorker: {WorkerId}", workerId);
                    var commandResult = await _workerManager.RemoveWorkerAsync(workerId);
                    return commandResult;
                });
                
                // Handle EngineAcknowledged command asynchronously
                hubConnection.On<string>("EngineAcknowledged", async (engineId) =>
                {
                    logger.LogInformation("hubConnection.On: Got EngineAcknowledged: {EngineId}", engineId);
                    // Synkroniser workers efter at engine er blevet accepteret
                    var workers = await _workerManager.GetAllWorkers(_engineInfo.EngineId);
                    await hubConnection.InvokeAsync("SynchronizeWorkers", workers, _engineInfo.EngineId);
                    await ProcessClientMessagesAsync(hubConnection, url, _cancellationTokenSource.Token);
                });
                
                hubConnection.On<string>("EngineRejected", async (reason) =>
                {
                    _logger.LogWarning("Connection rejected: {Reason}", reason);
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
        var syncTimestamp = DateTime.UtcNow; // Gem tidspunktet for synkroniseringen
        _hubConnectionSyncTimestamps[hubConnection] = syncTimestamp; // Opdater dictionary med timestamp for denne forbindelse

        Console.WriteLine($"----------Sending engine Init messages to streamHub on: {streamhubUrl}");
        
        // Hent opdateret engine-info
        _engineInfo = await _engineService.GetEngineAsync();


        var engineModel = new EngineBaseInfo
        {
            EngineId = _engineInfo.EngineId,
            EngineName = _engineInfo.Name,
            EngineDescription = _engineInfo.Description,
            Version = _engineInfo.Version,
            InstallDate = _engineInfo.InstallDate
        };

        Console.WriteLine($"{engineModel.EngineName} - {engineModel.Version}");
        // Send EngineConnected-besked til StreamHub, hvorefter vi afventer bekræftelse (EngineAcknowledged)
        await hubConnection.InvokeAsync("EngineConnected", engineModel);
    }

    // Global processing of messages from main queue to per-connection queue
    private async Task RouteMessagesToClientQueuesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start RouteMessagesToClientQueuesAsync ------Processing global queue to clients");
        while (!cancellationToken.IsCancellationRequested)
        {
            BaseMessage? baseMessage = await _globalMessageQueue.DequeueMessageAsync(cancellationToken);
            foreach (var hubConnection in _hubConnectionMessageQueue.Keys)
            {
                if (baseMessage is ImageData imageMessage)
                {
                    _hubConnectionMessageQueue[hubConnection]
                        .EnqueueMessage(imageMessage, $"IMAGE-{imageMessage.WorkerId.ToString()}");
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