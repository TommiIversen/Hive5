using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace StreamHub.Services;

public class BlazorSignalRService : IAsyncDisposable
{
    public BlazorSignalRService(NavigationManager navigationManager)
    {
        var baseUrl = Environment.GetEnvironmentVariable("SIGNALR_SERVER_URL") ?? navigationManager.BaseUri;
        var signalUri = new Uri($"{baseUrl}streamhub?clientType=frontend");

        Console.WriteLine($"SignalR URI: {signalUri}");
        
        HubConnection = new HubConnectionBuilder()
            .WithUrl(signalUri,
                options => { options.Transports = HttpTransportType.WebSockets; })
            .WithAutomaticReconnect()
            .Build();
    }

    public HubConnection HubConnection { get; }

    public async ValueTask DisposeAsync()
    {
        await HubConnection.DisposeAsync();
        Console.WriteLine("SignalR Async connection disposed.");
    }

    public async Task StartConnectionAsync()
    {
        if (HubConnection.State == HubConnectionState.Disconnected)
        {
            await HubConnection.StartAsync();
            Console.WriteLine("SignalR connected.");
        }
    }
}