using Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Engine.Hubs;

public static class MessageRouter
{
    public static async Task RouteMessageToClientAsync(HubConnection hubConnection, BaseMessage baseMessage)
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