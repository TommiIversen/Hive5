using Common.DTOs;
using Common.DTOs.Events;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace Engine.Hubs;

public static class MessageRouter
{
    public static async Task RouteMessageToClientAsync(IHubClient hubClient, BaseMessage baseMessage)
    {
        try
        {
            switch (baseMessage)
            {
                case Metric metric:
                    await hubClient.InvokeAsync("ReceiveMetric", metric);
                    break;
                case WorkerLogEntry log:
                    await hubClient.InvokeAsync("ReceiveWorkerLog", log);
                    break;
                case EngineLogEntry engineLog:
                    await hubClient.InvokeAsync("ReceiveEngineLog", engineLog);
                    break;
                case ImageData image:
                    await hubClient.InvokeAsync("ReceiveImage", image);
                    break;
                case WorkerChangeEvent workerEvent:
                    await hubClient.InvokeAsync("ReceiveWorkerEvent", workerEvent);
                    break;
                case EngineEvent engineEvent:
                    await hubClient.InvokeAsync("ReceiveEngineEvent", engineEvent);
                    break;
                default:
                    await HandleUnknownMessage(hubClient, baseMessage);
                    break;
            }
        }
        catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
        {
            Log.Warning(
                $"Method does not exist for message type: {baseMessage.GetType().Name}, redirecting to HandleUnknownMessage.");
            await HandleUnknownMessage(hubClient, baseMessage); // Redirect til ukendt beskedhåndtering
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to invoke method for message type: {baseMessage.GetType().Name}");
        }
    }

    private static async Task HandleUnknownMessage(IHubClient hubClient, BaseMessage baseMessage)
    {
        var message = $"Unknown message type received: {baseMessage.GetType().Name}, WorkerId: {baseMessage.EngineId}";
        Log.Warning(message);
        try
        {
            await hubClient.InvokeAsync("ReceiveDeadLetter", message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send ReceiveError to client");
        }
    }
}