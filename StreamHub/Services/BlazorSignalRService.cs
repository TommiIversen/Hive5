using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace StreamHub.Services;

public class BlazorSignalRService : IAsyncDisposable
{
    public HubConnection HubConnection { get; }

    public BlazorSignalRService(NavigationManager navigationManager)
    {
        HubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/streamhub?clientType=frontend"),
                options => { options.Transports = HttpTransportType.WebSockets; })
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task StartConnectionAsync()
    {
        if (HubConnection.State == HubConnectionState.Disconnected)
        {
            await HubConnection.StartAsync();
            Console.WriteLine("SignalR connected.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await HubConnection.DisposeAsync();
        Console.WriteLine("SignalR Async connection disposed.");
    }
}