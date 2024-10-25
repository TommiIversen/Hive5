using Common.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace Engine.Hubs;

public static class MessageRouter
{

    public static async Task RouteMessageToClientAsync(HubConnection hubConnection, BaseMessage baseMessage)
    {
        try
        {
            switch (baseMessage)
            {
                case Metric metric:
                    await hubConnection.InvokeAsync("ReceiveMetric", metric);
                    break;
                case WorkerLogEntry log:
                    await hubConnection.InvokeAsync("ReceiveWorkerLog", log);
                    break;
                case EngineLogEntry engineLog:
                    await hubConnection.InvokeAsync("ReceiveEngineLog", engineLog);
                    break;
                case ImageData image:
                    await hubConnection.InvokeAsync("ReceiveImage", image);
                    break;
                case WorkerEvent workerEvent:
                    await hubConnection.InvokeAsync("ReceiveWorkerEvent", workerEvent);
                    break;
                case EngineEvent engineEvent:
                    await hubConnection.InvokeAsync("ReceiveEngineEvent", engineEvent);
                    break;
                default:
                    await HandleUnknownMessage(hubConnection, baseMessage);
                    break;
            }
        }
        catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
        {
            Log.Warning($"Method does not exist for message type: {baseMessage.GetType().Name}, redirecting to HandleUnknownMessage.");
            await HandleUnknownMessage(hubConnection, baseMessage);  // Redirect til ukendt beskedhåndtering
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to invoke method for message type: {baseMessage.GetType().Name}");
        }
    }

    private static async Task HandleUnknownMessage(HubConnection hubConnection, BaseMessage baseMessage)
    {
        string message = $"Unknown message type received: {baseMessage.GetType().Name}, WorkerId: {baseMessage.EngineId}";
        Log.Warning(message);
        try
        {
            await hubConnection.InvokeAsync("ReceiveDeadLetter", message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send ReceiveError to client");
        }
    }
}
