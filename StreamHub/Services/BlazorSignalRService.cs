using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace StreamHub.Services;

public class BlazorSignalRService
{
    public HubConnection HubConnection { get; private set; }

    public BlazorSignalRService(NavigationManager navigationManager)
    {
        HubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/streamhub?clientType=frontend"), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
            })
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
}