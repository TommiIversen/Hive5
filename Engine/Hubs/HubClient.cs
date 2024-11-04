using Microsoft.AspNetCore.SignalR.Client;

namespace Engine.Hubs;

public interface IHubClient
{
    Task InvokeAsync(string methodName, params object[] args);
    Task InvokeAsync<T>(string methodName, T arg);
}

public class HubClient : IHubClient
{
    private readonly HubConnection _hubConnection;

    public HubClient(HubConnection hubConnection)
    {
        _hubConnection = hubConnection;
    }

    public Task InvokeAsync(string methodName, params object[] args)
    {
        return _hubConnection.InvokeAsync(methodName, args);
    }

    public Task InvokeAsync<T>(string methodName, T arg)
    {
        return _hubConnection.InvokeAsync(methodName, arg);
    }
}