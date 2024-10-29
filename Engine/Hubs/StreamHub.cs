using System.Collections.Concurrent;
using Common.DTOs;
using Engine.Services;
using Engine.Utils;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace Engine.Hubs;



public class StreamHub 
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly IEngineService _engineService;

    private readonly MessageQueue _globalMessageQueue;
    private readonly ConcurrentDictionary<string, HubConnectionInfo> _hubConnections = new();

    // fild to init date time
    private readonly DateTime _initDateTime = DateTime.UtcNow;

    private readonly ILogger<StreamHub> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly int _maxQueueSize = 20;
    private readonly IWorkerManager _workerManager;
    private readonly Guid _engineId;
    private readonly ILoggerService _loggerService;


    public StreamHub(
        ILoggerService loggerService, 
        MessageQueue globalMessageQueue,
        ILogger<StreamHub> logger,
        ILoggerFactory loggerFactory,
        IWorkerManager workerManager,
        IEngineService engineService
        )
    {
        _loggerService = loggerService;

        _logger = logger;
        _globalMessageQueue = globalMessageQueue;
        _workerManager = workerManager;
        _engineService = engineService;
        _loggerFactory = loggerFactory;

        var engineInfo = GetEngineBaseInfo().Result;
        _engineId = engineInfo.EngineId;
        foreach (var hubUrl in engineInfo.HubUrls.Select(h => h.HubUrl)) StartHubConnection(hubUrl);

        _ = Task.Run(async () => await RouteMessagesToClientQueuesAsync(_cancellationTokenSource.Token));
    }

    // public void AddHubUrl(string newHubUrl)
    // {
    //     try
    //     {
    //         StartHubConnection(newHubUrl);
    //
    //         // Tilføj URL'en til databasen
    //         var hubUrlEntity = new HubUrlEntity { HubUrl = newHubUrl };
    //         _engineInfo.HubUrls.Add(hubUrlEntity);
    //
    //         // Gem ændringerne i databasen
    //         _engineService.UpdateEngineAsync(_engineInfo.EngineName, _engineInfo.EngineDescription).Wait();
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to add and start hub connection for {Url}", newHubUrl);
    //     }
    // }

    private async Task<CommandResult> RemoveHubUrlAsync(string hubUrlToRemove)
    {
        //Console.WriteLine($"Removing hub connection for URL: {hubUrlToRemove}");
        LogInfo($"Removing hub connection for URL: {hubUrlToRemove}");

        // Hvis forbindelsen findes, skal vi stoppe den og fjerne den fra hubConnections
        if (_hubConnections.TryGetValue(hubUrlToRemove, out var connectionInfo))
            try
            {
                // Annullér retry-loopet ved at annullere token source
                await connectionInfo.ReconnectTokenSource.CancelAsync();

                // Stop forbindelsen, hvis den er aktiv
                await connectionInfo.HubConnection.StopAsync();
                _hubConnections.TryRemove(hubUrlToRemove, out _);
                //_logger.LogInformation("Stopped and removed active hub connection for {Url}", hubUrlToRemove);
                LogInfo($"Stopped and removed active hub connection for {hubUrlToRemove}");
            }
            catch (Exception ex)
            {
                LogInfo($"Failed to stop and remove hub connection for {hubUrlToRemove}", LogLevel.Error);
                return new CommandResult(false, $"Failed to stop hub connection: {ex.Message}");
            }
        else
            LogInfo($"No active connection found for {hubUrlToRemove}, proceeding to remove it from database.", LogLevel.Error);

        // Uanset om forbindelsen var aktiv eller ej, skal vi stadig fjerne URL'en fra databasen
        try
        {
            var engine = await _engineService.GetEngineAsync();
            var hubUrlEntity = engine?.HubUrls.FirstOrDefault(h => h.HubUrl == hubUrlToRemove);

            if (hubUrlEntity == null)
            {
                LogInfo($"Hub URL {hubUrlToRemove} not found in the database.", LogLevel.Warning);
                return new CommandResult(false, $"Hub URL {hubUrlToRemove} not found in the database.");
            }

            await _engineService.RemoveHubUrlAsync(hubUrlEntity.Id);
            LogInfo($"Successfully removed hub URL {hubUrlToRemove} from database via EngineService");

            var engineUpdateEvent = await GetEngineBaseInfo();
            _globalMessageQueue.EnqueueMessage(engineUpdateEvent);

            return new CommandResult(true, $"Successfully removed hub URL {hubUrlToRemove} from database.");
        }
        catch (Exception ex)
        {
            LogInfo($"Failed to remove hub URL {hubUrlToRemove} from database: {ex.Message}", LogLevel.Error);
            return new CommandResult(false, $"Failed to remove hub URL {hubUrlToRemove} from database: {ex.Message}");
        }
    }

    private void StartHubConnection(string hubUrl)
    {
        try
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl($"{hubUrl}?clientType=backend", connectionOptions =>
                {
                    connectionOptions.Transports = HttpTransportType.WebSockets; // Kun WebSockets
                })
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5)
                })
                .AddMessagePackProtocol()
                .Build();

            // Handle StopWorker command asynchronously
            hubConnection.On("StopWorker", async (WorkerOperationMessage message) =>
            {
                LogInfo($"Got StopWorker: {message.WorkerId}");
                var commandResult = await _workerManager.StopWorkerAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("StartWorker", async (WorkerOperationMessage message) =>
            {
                LogInfo($"Got StartWorker: {message.WorkerId}");
                var commandResult = await _workerManager.StartWorkerAsync(message.WorkerId);
                return commandResult;
            });

            // Handle RemoveWorker command asynchronously
            hubConnection.On("RemoveWorker", async (WorkerOperationMessage message) =>
            {
                LogInfo($"Got RemoveWorker: {message.WorkerId}");
                var commandResult = await _workerManager.RemoveWorkerAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("RemoveHubConnection", async (string hubUrlToRemove) =>
            {
                LogInfo($"Got request to remove HubUrl: {hubUrlToRemove}");
                var commandResult = await RemoveHubUrlAsync(hubUrlToRemove);
                return commandResult;
            });

            hubConnection.On("ResetWatchdogEventCount", async (WorkerOperationMessage message) =>
            {
                LogInfo($"Got ResetWatchdogEventCount: {message.WorkerId}");
                var commandResult = await _workerManager.ResetWatchdogEventCountAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("EnableDisableWorker", async (WorkerEnableDisableMessage message) =>
            {
                LogInfo($"Got EnableDisableWorker: {message.WorkerId}, Enable: {message.Enable}");
                var commandResult = await _workerManager.EnableDisableWorkerAsync(message.WorkerId, message.Enable);
                return commandResult;
            });

            hubConnection.On("EditWorker", async (WorkerCreate workerEdit) =>
            {
                LogInfo($"Got EditWorker for WorkerId: {workerEdit.WorkerId} - {workerEdit.Name}");
                var commandResult = await _workerManager.EditWorkerAsync(workerEdit.WorkerId, workerEdit.Name,
                    workerEdit.Description, workerEdit.Command);
                return commandResult;
            });

            // Add new SignalR handler for creating workers
            hubConnection.On("CreateWorker", async (WorkerCreate workerCreate) =>
            {
                LogInfo($"Got CreateWorker: {workerCreate.Name}");
                var workerService = await _workerManager.AddWorkerAsync(_engineId, workerCreate);
                if (workerService == null)
                {
                    LogInfo($"Worker with ID {workerCreate.WorkerId} already exists.", LogLevel.Warning);
                    return new CommandResult(false, $"Worker with ID {workerCreate.WorkerId} already exists.");
                }
                var result = await _workerManager.StartWorkerAsync(workerService.WorkerId);
                return result;
            });

            hubConnection.Reconnected += async _ =>
            {
                LogInfo($"Reconnected to streamhub {hubUrl} - {hubConnection.ConnectionId}");
                await SendEngineConnectedAsync(hubConnection, hubUrl);
            };

            hubConnection.Closed += async error =>
            {
                LogInfo($"Connection closed: {error?.Message}", LogLevel.Warning);
                await Task.Delay(5000); // Vent før næste forsøg
                await TryReconnect(hubConnection, hubUrl, _cancellationTokenSource.Token);
            };

            // Opret ny HubConnectionInfo
            var connectionInfo = new HubConnectionInfo
            {
                HubConnection = hubConnection,
                MessageQueue = new MultiQueue(_loggerFactory.CreateLogger<MultiQueue>(), _maxQueueSize),
                SyncTimestamp = DateTime.MinValue,
                ReconnectTokenSource = new CancellationTokenSource() // Opret ny token source for denne forbindelse
            };

            _hubConnections[hubUrl] = connectionInfo;

            _ = Task.Run(async () =>
                await TryReconnect(hubConnection, hubUrl, connectionInfo.ReconnectTokenSource.Token));
        }
        catch (Exception ex)
        {
            LogInfo($"Failed to connect to {hubUrl}", LogLevel.Error);
        }
    }

    private async Task TryReconnect(HubConnection hubConnection, string url, CancellationToken token)
    {
        while (true)
            try
            {
                await hubConnection.StartAsync(token);
                LogInfo($"Connected to {url} {hubConnection.ConnectionId}");
                await SendEngineConnectedAsync(hubConnection, url);
                break; // Stop genforbindelses-loopet, når forbindelsen er oprettet
            }
            catch when (token.IsCancellationRequested)
            {
                break; // Stop, hvis token er annulleret
            }
            catch (Exception)
            {
                LogInfo($"Failed to connect to {url}. Retrying in 5 seconds...", LogLevel.Warning);
                await Task.Delay(5000, token); // Vent før næste forsøg
            }
    }


    private async Task SendEngineConnectedAsync(HubConnection hubConnection, string streamhubUrl)
    {
        if (_hubConnections.TryGetValue(streamhubUrl, out var connectionInfo))
        {
            connectionInfo.SyncTimestamp = DateTime.UtcNow;
            LogInfo($"Sending engine Init messages to streamHub on: {streamhubUrl}");

            var engineModel = await GetEngineBaseInfo();
            var connectedAndAccepted = await hubConnection.InvokeAsync<bool>("RegisterEngineConnection", engineModel);

            if (connectedAndAccepted)
            {
                LogInfo("Connection to StreamHub acknowledged, synchronizing workers...");
                var systemInfoCollector = new SystemInfoCollector();
                var systemInfo = systemInfoCollector.GetSystemInfo();
                systemInfo.EngineId = _engineId;
                try
                {
                    await hubConnection.InvokeAsync("SendSystemInfo", systemInfo);
                    LogInfo("System information blev sendt succesfuldt via SignalR.");
                }
                catch (Exception ex)
                {
                    LogInfo("Fejl opstod under forsøget på at sende systeminformation via SignalR.", LogLevel.Error);
                }

                var workers = await _workerManager.GetAllWorkers(_engineId);
                await hubConnection.InvokeAsync("SynchronizeWorkers", workers, _engineId);
                await ProcessClientMessagesAsync(hubConnection, streamhubUrl, _cancellationTokenSource.Token);
            }
            else
            {
                LogInfo("Connection to StreamHub rejected.", LogLevel.Error);
                await hubConnection.StopAsync();
            }
        }
        else
        {
            LogInfo($"No connection info found for {streamhubUrl}", LogLevel.Error);
        }
    }

    private async Task<EngineEvent> GetEngineBaseInfo()
    {
        var engine = await _engineService.GetEngineAsync(); // Hent de opdaterede data
        if (engine == null) throw new InvalidOperationException("Engine not found");

        var engineBaseInfo = new EngineEvent
        {
            EngineId = engine.EngineId,
            EngineName = engine.Name,
            EngineDescription = engine.Description,
            Version = engine.Version,
            InstallDate = engine.InstallDate,
            EngineStartDate = _initDateTime,
            HubUrls = engine.HubUrls.Select(h => new HubUrlInfo
                {
                    Id = h.Id,
                    HubUrl = h.HubUrl,
                    ApiKey = h.ApiKey
                })
                .ToList(),
            EventType = EventType.Updated // Mapper de nyeste URL'er fra database til DTO
        };

        return engineBaseInfo;
    }

    // Global processing of messages from main queue to per-connection queue
    private async Task RouteMessagesToClientQueuesAsync(CancellationToken cancellationToken)
    {
        LogInfo("Start RouteMessagesToClientQueuesAsync - Processing global queue to clients");
        while (!cancellationToken.IsCancellationRequested)
        {
            var baseMessage = await _globalMessageQueue.DequeueMessageAsync(cancellationToken);
            foreach (var connectionInfo in _hubConnections.Values)
            {
                if (baseMessage is ImageData imageMessage)
                {
                    connectionInfo.MessageQueue.EnqueueMessage(imageMessage, $"IMAGE-{imageMessage.WorkerId}");
                    continue;
                }

                connectionInfo.MessageQueue.EnqueueMessage(baseMessage);
            }
        }
    }

    // Process the queue for each streamhub signalR connection independently
    private async Task ProcessClientMessagesAsync(HubConnection hubConnection, string url,
        CancellationToken cancellationToken)
    {
        LogInfo($"Start ProcessClientMessagesAsync - Processing hub queue to {url}");
        if (_hubConnections.TryGetValue(url, out var connectionInfo))
        {
            var queue = connectionInfo.MessageQueue;
            var sequenceNumber = 0;

            // Brug synkroniseringstidspunktet fra connectionInfo
            var syncTimestamp = connectionInfo.SyncTimestamp;

            while (!cancellationToken.IsCancellationRequested)
            {
                var baseMessage = await queue.DequeueMessageAsync(cancellationToken);

                // Filtrer forældede WorkerEvent-beskeder baseret på syncTimestamp
                if (baseMessage is WorkerEvent workerEvent && workerEvent.Timestamp < syncTimestamp)
                {
                    LogInfo(
                        $"Skipping outdated event for streamHub: {url} - {workerEvent.EventType} {workerEvent.Name}");
                    continue;
                }

                if (hubConnection.State == HubConnectionState.Connected)
                {
                    EnrichMessage(baseMessage, sequenceNumber++);
                    await MessageRouter.RouteMessageToClientAsync(hubConnection, baseMessage);
                }
                else
                {
                    LogInfo($"Hub {url} is offline, stopping ProcessClientMessagesAsync", LogLevel.Warning);
                    break;
                }
            }
        }
        else
        {
            LogInfo($"No connection info found for {url}", LogLevel.Error);
        }
    }


    private void EnrichMessage(BaseMessage baseMessage, int sequenceNumber)
    {
        baseMessage.EngineId = _engineId;
        baseMessage.SequenceNumber = sequenceNumber;
    }
    
    private void LogInfo(string message, LogLevel logLevel = LogLevel.Information)
    {
        _loggerService.LogMessage(new EngineLogEntry()
        {
            Message = $"StreamHub: {message}",
            LogLevel = logLevel
        });
    }
}