using System.Collections.Concurrent;
using Common.DTOs;

using Engine.Services;
using Engine.Utils;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace Engine.Hubs;

public class HubConnectionInfo
{
    public required HubConnection HubConnection { get; set; }
    public required MultiQueue MessageQueue { get; set; }
    public DateTime SyncTimestamp { get; set; } = DateTime.MinValue;
    public CancellationTokenSource ReconnectTokenSource { get; set; } = new();
}

public class StreamHub
{
    private readonly ConcurrentDictionary<string, HubConnectionInfo> _hubConnections = new();

    private readonly MessageQueue _globalMessageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly ILogger<StreamHub> _logger;
    private readonly WorkerManager _workerManager;
    private readonly int _maxQueueSize = 20;
    private readonly ILoggerFactory _loggerFactory;

    private readonly IEngineService _engineService;
    private Guid _engineId;

    // fild to init date time
    private readonly DateTime _initDateTime = DateTime.UtcNow;

    public StreamHub(
        MessageQueue globalMessageQueue,
        ILogger<StreamHub> logger,
        ILoggerFactory loggerFactory,
        WorkerManager workerManager,
        IEngineService engineService)
    {
        _logger = logger;
        _globalMessageQueue = globalMessageQueue;
        _workerManager = workerManager;
        _engineService = engineService;
        _loggerFactory = loggerFactory;

        var engineInfo = GetEngineBaseInfo().Result;
        _engineId = engineInfo.EngineId;
        foreach (var hubUrl in engineInfo.HubUrls.Select(h => h.HubUrl))
        {
            StartHubConnection(hubUrl);
        }

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
        Console.WriteLine($"Removing hub connection for URL: {hubUrlToRemove}");

        // Hvis forbindelsen findes, skal vi stoppe den og fjerne den fra hubConnections
        if (_hubConnections.TryGetValue(hubUrlToRemove, out var connectionInfo))
        {
            try
            {
                // Annullér retry-loopet ved at annullere token source
                await connectionInfo.ReconnectTokenSource.CancelAsync();

                // Stop forbindelsen, hvis den er aktiv
                await connectionInfo.HubConnection.StopAsync();
                _hubConnections.TryRemove(hubUrlToRemove, out _);
                _logger.LogInformation("Stopped and removed active hub connection for {Url}", hubUrlToRemove);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop and remove hub connection for {Url}", hubUrlToRemove);
                return new CommandResult(false, $"Failed to stop hub connection: {ex.Message}");
            }
        }
        else
        {
            _logger.LogWarning("No active connection found for {Url}, proceeding to remove it from database.",
                hubUrlToRemove);
        }

        // Uanset om forbindelsen var aktiv eller ej, skal vi stadig fjerne URL'en fra databasen
        try
        {
            var engine = await _engineService.GetEngineAsync();
            var hubUrlEntity = engine?.HubUrls.FirstOrDefault(h => h.HubUrl == hubUrlToRemove);

            if (hubUrlEntity != null)
            {
                await _engineService.RemoveHubUrlAsync(hubUrlEntity
                    .Id); // Brug EngineService til at fjerne URL'en fra databasen
                _logger.LogInformation("Successfully removed hub URL {Url} from database via EngineService",
                    hubUrlToRemove);

                var engineUpdateEvent = await GetEngineBaseInfo(); // Opdateret engine data til event

                // loop over urls in engineInfo
                foreach (var hubUrl in engineUpdateEvent.HubUrls.Select(h => h.HubUrl))
                {
                    Console.WriteLine($"Updated URL: {hubUrl}");
                }

                _globalMessageQueue.EnqueueMessage(engineUpdateEvent); // Send opdatering til alle forbindelser

                return new CommandResult(true, $"Successfully removed hub URL {hubUrlToRemove} from database.");
            }
            else
            {
                _logger.LogWarning("Hub URL {Url} not found in the database.", hubUrlToRemove);
                return new CommandResult(false, $"Hub URL {hubUrlToRemove} not found in the database.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove hub URL {Url} from database.", hubUrlToRemove);
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
                .Build();

            // Handle StopWorker command asynchronously
            hubConnection.On("StopWorker", async (WorkerOperationMessage message) =>
            {
                _logger.LogInformation("hubConnection.On: Got StopWorker: {WorkerId}", message.WorkerId);
                var commandResult = await _workerManager.StopWorkerAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("StartWorker", async (WorkerOperationMessage message) =>
            {
                _logger.LogInformation("hubConnection.On: Got StartWorker: {WorkerId}", message.WorkerId);
                var commandResult = await _workerManager.StartWorkerAsync(message.WorkerId);
                return commandResult;
            });

            // Handle RemoveWorker command asynchronously
            hubConnection.On("RemoveWorker", async (WorkerOperationMessage message) =>
            {
                _logger.LogInformation("hubConnection.On: Got RemoveWorker: {WorkerId}", message.WorkerId);
                var commandResult = await _workerManager.RemoveWorkerAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("RemoveHubConnection", async (string hubUrlToRemove) =>
            {
                _logger.LogInformation("hubConnection.On: Got request to remove HubUrl: {HubUrl}", hubUrlToRemove);
                var commandResult = await RemoveHubUrlAsync(hubUrlToRemove);
                return commandResult;
            });

            // Handle ResetWatchdogEventCount command asynchronously
            hubConnection.On("ResetWatchdogEventCount", async (WorkerOperationMessage message) =>
            {
                _logger.LogInformation("hubConnection.On: Got ResetWatchdogEventCount: {WorkerId}", message.WorkerId);
                var commandResult = await _workerManager.ResetWatchdogEventCountAsync(message.WorkerId);
                _logger.LogInformation("Reset Watchdog Event Count Result for worker {WorkerId}: {Message}",
                    message.WorkerId, commandResult.Message);
                return commandResult;
            });

            hubConnection.On("EnableDisableWorker", async (WorkerEnableDisableMessage message) =>
            {
                _logger.LogInformation("hubConnection.On: Got EnableDisableWorker: {WorkerId}, Enable: {Enable}",
                    message.WorkerId, message.Enable);
                var commandResult = await _workerManager.EnableDisableWorkerAsync(message.WorkerId, message.Enable);
                return commandResult;
            });

            hubConnection.On("EditWorker", async (WorkerCreate workerEdit) =>
            {
                _logger.LogInformation("hubConnection.On: Got EditWorker for WorkerId: {WorkerId} - {name}",
                    workerEdit.WorkerId, workerEdit.Name);
                var commandResult = await _workerManager.EditWorkerAsync(workerEdit.WorkerId, workerEdit.Name,
                    workerEdit.Description, workerEdit.Command);
                _logger.LogInformation("Edit Worker Result for worker {WorkerId}: {Message}", workerEdit.WorkerId,
                    commandResult.Message);
                return commandResult;
            });

            // Add new SignalR handler for creating workers
            hubConnection.On("CreateWorker", async (WorkerCreate workerCreate) =>
            {
                _logger.LogInformation("hubConnection.On: Got CreateWorker: {WorkerName}", workerCreate.Name);
                var workerService = await _workerManager.AddWorkerAsync(_engineId, workerCreate);
                if (workerService == null)
                {
                    _logger.LogWarning("Worker with ID {WorkerId} already exists.", workerCreate.WorkerId);
                    return new CommandResult(false, $"Worker with ID {workerCreate.WorkerId} already exists.");
                }
                var result = await _workerManager.StartWorkerAsync(workerService.WorkerId);
                return result;
            });

            hubConnection.Reconnected += async (_) =>
            {
                _logger.LogInformation("hubConnection:: Reconnected to streamhub {url} - {ConnectionId}", hubUrl,
                    hubConnection.ConnectionId);
                await SendEngineConnectedAsync(hubConnection, hubUrl);
            };

            hubConnection.Closed += async (error) =>
            {
                _logger.LogWarning("Connection closed: {Error}", error?.Message);
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
            _logger.LogError(ex, "Failed to connect to {Url}", hubUrl);
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
        if (_hubConnections.TryGetValue(streamhubUrl, out var connectionInfo))
        {
            connectionInfo.SyncTimestamp = DateTime.UtcNow;
            Console.WriteLine($"----------Sending engine Init messages to streamHub on: {streamhubUrl}");

            var engineModel = await GetEngineBaseInfo();
            bool connectedAndAccepted = await hubConnection.InvokeAsync<bool>("RegisterEngineConnection", engineModel);

            if (connectedAndAccepted)
            {
                Console.WriteLine(" ---------- Connection to StreamHub acknowledged, synchronizing workers...");
                
                var systemInfoCollector = new SystemInfoCollector();
                var systemInfo = systemInfoCollector.GetSystemInfo();
                systemInfo.EngineId = _engineId;
                Console.WriteLine($"SystemInfo: {systemInfo.OsName} {systemInfo.OSVersion} {systemInfo.Architecture} {systemInfo.Uptime} {systemInfo.ProcessCount} {systemInfo.Platform}");
                
                try
                {
                    await hubConnection.InvokeAsync("SendSystemInfo", systemInfo);
                    Log.Information("System information blev sendt succesfuldt via SignalR.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Fejl opstod under forsøget på at sende systeminformation via SignalR.");
                }
                
                var workers = await _workerManager.GetAllWorkers(_engineId);
                await hubConnection.InvokeAsync("SynchronizeWorkers", workers, _engineId);
                await ProcessClientMessagesAsync(hubConnection, streamhubUrl, _cancellationTokenSource.Token);
            }
            else
            {
                Console.WriteLine("Connection to StreamHub rejected.");
                // Eventuelt stoppe forbindelsen eller tage anden handling
                await hubConnection.StopAsync();
            }
        }
        else
        {
            _logger.LogWarning("No connection info found for {streamhubUrl}", streamhubUrl);
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
        Console.WriteLine($"Start RouteMessagesToClientQueuesAsync ------Processing global queue to clients");
        while (!cancellationToken.IsCancellationRequested)
        {
            BaseMessage baseMessage = await _globalMessageQueue.DequeueMessageAsync(cancellationToken);
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
        Console.WriteLine($"Start ProcessClientMessagesAsync ------Processing hub queue to {_engineId}");

        if (_hubConnections.TryGetValue(url, out var connectionInfo))
        {
            var queue = connectionInfo.MessageQueue;
            var sequenceNumber = 0;

            // Brug synkroniseringstidspunktet fra connectionInfo
            var syncTimestamp = connectionInfo.SyncTimestamp;

            while (!cancellationToken.IsCancellationRequested)
            {
                BaseMessage baseMessage = await queue.DequeueMessageAsync(cancellationToken);

                // Filtrer forældede WorkerEvent-beskeder baseret på syncTimestamp
                if (baseMessage is WorkerEvent workerEvent && workerEvent.Timestamp < syncTimestamp)
                {
                    Console.WriteLine(
                        $"✂Skipping outdated event for streamHub: {url} - {workerEvent.EventType} {workerEvent.Name}");
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
        else
        {
            _logger.LogWarning("No connection info found for {url}", url);
        }
    }


    private void EnrichMessage(BaseMessage baseMessage, int sequenceNumber)
    {
        baseMessage.EngineId = _engineId;
        baseMessage.SequenceNumber = sequenceNumber;
    }
}