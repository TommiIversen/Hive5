using System.Collections.Concurrent;
using Common.DTOs.Commands;
using Common.DTOs.Events;
using Engine.Interfaces;
using Engine.Services;
using Engine.Utils;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Engine.Hubs;

public class StreamHub
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Guid _engineId;
    private readonly IEngineService _engineService;
    private readonly IMessageQueue _globalMessageQueue;
    private readonly ConcurrentDictionary<string, HubConnectionInfo> _hubConnections = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILoggerService _loggerService;
    private readonly int _maxQueueSize = 20;
    private readonly IMessageEnricher _messageEnricher;
    private readonly IHubWorkerEventHandlers _workerEventHandlers;
    private readonly IWorkerManager _workerManager;


    public StreamHub(ILoggerService loggerService,
        IMessageQueue globalMessageQueue,
        ILoggerFactory loggerFactory,
        IWorkerManager workerManager,
        IEngineService engineService,
        IMessageEnricher messageEnricher,
        IHubWorkerEventHandlers workerEventHandlers)
    {
        _loggerService = loggerService;
        _messageEnricher = messageEnricher;
        _globalMessageQueue = globalMessageQueue;
        _workerManager = workerManager;
        _engineService = engineService;
        _loggerFactory = loggerFactory;
        _workerEventHandlers = workerEventHandlers;

        var engineInfo = _engineService.GetEngineBaseInfoAsEvent().Result;
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
            LogInfo($"No active connection found for {hubUrlToRemove}, proceeding to remove it from database.",
                LogLevel.Error);

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

            var engineUpdateEvent = await _engineService.GetEngineBaseInfoAsEvent();
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
            var messagePackOptions = MessagePackSerializerOptions.Standard
                .WithResolver(TypelessContractlessStandardResolver.Instance);
            var hubConnection = new HubConnectionBuilder()
                .WithUrl($"{hubUrl}?clientType=backend",
                    connectionOptions => { connectionOptions.Transports = HttpTransportType.WebSockets; })
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5)
                })
                .AddMessagePackProtocol()
                .Build();


            hubConnection.On("RemoveHubConnection", async (string hubUrlToRemove) =>
            {
                LogInfo($"Got request to remove HubUrl: {hubUrlToRemove}");
                var commandResult = await RemoveHubUrlAsync(hubUrlToRemove);
                return commandResult;
            });

            _workerEventHandlers.AttachWorkerHandlers(hubConnection); // Vedhæft worker-specifikke handlers

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
        catch (Exception)
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

            var engineModel = await _engineService.GetEngineBaseInfoAsEvent();
            var connectedAndAccepted = await hubConnection.InvokeAsync<bool>("RegisterEngineConnection", engineModel);

            if (connectedAndAccepted)
            {
                LogInfo("Connection to StreamHub acknowledged, synchronizing workers...");
                var systemInfoCollector = new SystemInfoCollector();
                var systemInfo = systemInfoCollector.GetSystemInfo();
                systemInfo.EngineId = _engineId;
                try
                {
                    await hubConnection.InvokeAsync("ReceiveEngineSystemInfo", systemInfo);
                    LogInfo("System information blev sendt succesfuldt via SignalR.");
                }
                catch (Exception)
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

    // Global processing of messages from main queue to per-connection queue
    private async Task RouteMessagesToClientQueuesAsync(CancellationToken cancellationToken)
    {
        LogInfo("Start RouteMessagesToClientQueuesAsync - Processing global queue to clients");
        while (!cancellationToken.IsCancellationRequested)
        {
            var baseMessage = await _globalMessageQueue.DequeueMessageAsync(cancellationToken);
            foreach (var connectionInfo in _hubConnections.Values)
            {
                if (baseMessage is WorkerImageData imageMessage)
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
            // Brug synkroniseringstidspunktet fra connectionInfo
            var syncTimestamp = connectionInfo.SyncTimestamp;

            IHubClient hubClient = new HubClient(hubConnection);


            while (!cancellationToken.IsCancellationRequested)
            {
                var baseMessage = await queue.DequeueMessageAsync(cancellationToken);

                // Filtrer forældede WorkerEvent-beskeder baseret på syncTimestamp
                if (baseMessage is WorkerChangeEvent workerEvent && workerEvent.Timestamp < syncTimestamp)
                {
                    LogInfo(
                        $"Skipping outdated event for streamHub: {url} - {workerEvent.ChangeEventType} {workerEvent.Name}");
                    continue;
                }

                if (hubConnection.State == HubConnectionState.Connected)
                {
                    _messageEnricher.Enrich(baseMessage, _engineId);
                    await MessageRouter.RouteMessageToClientAsync(hubClient, baseMessage);
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


    private void LogInfo(string message, LogLevel logLevel = LogLevel.Information)
    {
        _loggerService.LogMessage(new EngineLogEntry
        {
            LogTimestamp = DateTime.UtcNow,
            Message = $"StreamHub: {message}",
            LogLevel = logLevel
        });
    }
}