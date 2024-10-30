using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using Engine.Services;
using Microsoft.AspNetCore.Http.Connections;

namespace Engine.Hubs
{
    public interface IHubConnectionManager
    {
        Task StartConnectionAsync(string hubUrl, CancellationToken cancellationToken);
        Task StopConnectionAsync(string hubUrl);
        HubConnectionInfo GetConnectionInfo(string hubUrl);
        ConcurrentDictionary<string, HubConnectionInfo> GetAllConnections();
    }

    public class HubConnectionManager : IHubConnectionManager
    {
        private readonly ConcurrentDictionary<string, HubConnectionInfo> _hubConnections = new();
        private readonly ILogger<MultiQueue> _logger;

        public HubConnectionManager(ILogger<MultiQueue> logger)
        {
            _logger = logger;
        }

        public async Task StartConnectionAsync(string hubUrl, CancellationToken cancellationToken)
        {
            if (_hubConnections.ContainsKey(hubUrl))
            {
                _logger.LogWarning($"Connection to {hubUrl} already exists.");
                return;
            }

            var hubConnection = new HubConnectionBuilder()
                .WithUrl($"{hubUrl}?clientType=backend", options =>
                {
                    options.Transports = HttpTransportType.WebSockets; // Bruger kun WebSockets
                })
                .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5) })
                .AddMessagePackProtocol()
                .Build();
            

            // Event handlers til genforbindelse og lukning
            hubConnection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation($"Reconnected to {hubUrl} - ConnectionId: {connectionId}");
                await OnReconnected(hubUrl);
            };

            hubConnection.Closed += async (error) =>
            {
                _logger.LogWarning($"Connection closed: {error?.Message}. Retrying in 5 seconds...");
                await Task.Delay(5000, cancellationToken);
                await TryReconnect(hubUrl, cancellationToken);
            };

            var connectionInfo = new HubConnectionInfo
            {
                HubConnection = hubConnection,
                MessageQueue = new MultiQueue(_logger, 20),
                ReconnectTokenSource = new CancellationTokenSource()
            };

            _hubConnections[hubUrl] = connectionInfo;

            await TryReconnect(hubUrl, cancellationToken);
        }

        private async Task TryReconnect(string hubUrl, CancellationToken cancellationToken)
        {
            if (!_hubConnections.TryGetValue(hubUrl, out var connectionInfo))
                return;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await connectionInfo.HubConnection.StartAsync(cancellationToken);
                    _logger.LogInformation($"Connected to {hubUrl} with ConnectionId: {connectionInfo.HubConnection.ConnectionId}");
                    await OnConnected(hubUrl);
                    break; // Stop genforbindelses-loopet, når forbindelsen er oprettet
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning($"Failed to connect to {hubUrl}. Retrying in 5 seconds... Error: {ex.Message}");
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }

        public async Task StopConnectionAsync(string hubUrl)
        {
            if (_hubConnections.TryRemove(hubUrl, out var connectionInfo))
            {
                try
                {
                    connectionInfo.ReconnectTokenSource.Cancel(); // Annullér genforbindelses-tokens
                    await connectionInfo.HubConnection.StopAsync();
                    _logger.LogInformation($"Stopped and removed active hub connection for {hubUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to stop and remove hub connection for {hubUrl}: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning($"No active connection found for {hubUrl} to stop.");
            }
        }

        private async Task OnConnected(string hubUrl)
        {
            if (_hubConnections.TryGetValue(hubUrl, out var connectionInfo))
            {
                connectionInfo.SyncTimestamp = DateTime.UtcNow;
                _logger.LogInformation($"Connection initialized and SyncTimestamp set for {hubUrl}");
                
                // Ekstra funktioner som at sende initialiseringer kan tilføjes her
            }
        }

        private async Task OnReconnected(string hubUrl)
        {
            if (_hubConnections.TryGetValue(hubUrl, out var connectionInfo))
            {
                connectionInfo.SyncTimestamp = DateTime.UtcNow;
                _logger.LogInformation($"Reconnection initialized and SyncTimestamp updated for {hubUrl}");

                // Ekstra funktioner som at sende initialiseringer kan tilføjes her
            }
        }

        public HubConnectionInfo GetConnectionInfo(string hubUrl)
        {
            _hubConnections.TryGetValue(hubUrl, out var connectionInfo);
            return connectionInfo;
        }

        public ConcurrentDictionary<string, HubConnectionInfo> GetAllConnections() => _hubConnections;
    }
}
