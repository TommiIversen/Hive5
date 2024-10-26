using Engine.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace Engine.Hubs;

public class HubConnectionInfo
{
    public required HubConnection HubConnection { get; set; }
    public required MultiQueue MessageQueue { get; set; }
    public DateTime SyncTimestamp { get; set; } = DateTime.MinValue;
    public CancellationTokenSource ReconnectTokenSource { get; set; } = new();
}