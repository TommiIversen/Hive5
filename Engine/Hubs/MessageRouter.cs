using Common.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

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
            
            case WorkerEvent workerEvent:
                await hubConnection.InvokeAsync("ReceiveWorkerEvent", workerEvent);
                break;
            
            default:
                HandleUnknownMessage(hubConnection, baseMessage);
                break;
        }
    }
    
    
    private static async void HandleUnknownMessage(HubConnection hubConnection, BaseMessage baseMessage)
    {
        Console.WriteLine($"Unknown message type received: {baseMessage.GetType().Name}");
        Log.Warning($"Unknown message type received: {baseMessage.GetType().Name}, WorkerId: {baseMessage.EngineId}");
        try
        {
            await hubConnection.InvokeAsync("ReceiveDeadLetter", new
            {
                Message = "Unknown message type",
                ReceivedMessageType = baseMessage.GetType().Name,
                WorkerId = baseMessage.EngineId,
                Timestamp = baseMessage.Timestamp
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send ReceiveError to client");
        }
    }
}