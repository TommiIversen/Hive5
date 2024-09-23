namespace StreamHub.Services;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

public class TrackingCircuitHandler : CircuitHandler
{
    private readonly ILogger<TrackingCircuitHandler> _logger;
    
    // Thread-safe dictionary to keep track of user connections
    private readonly ConcurrentDictionary<string, bool> _connectedCircuits = new();
    public event Action? OnUserCountChanged;


    public TrackingCircuitHandler(ILogger<TrackingCircuitHandler> logger)
    {
        _logger = logger;
    }

    // Called when a circuit is connected
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Add the circuit to the dictionary with the value 'true' (indicating it's connected)
        _connectedCircuits[circuit.Id] = true;
        OnUserCountChanged?.Invoke(); // Trigger event

        
        // Log the connection
        _logger.LogInformation("Circuit {CircuitId} connected. Total connections: {TotalConnections}", 
            circuit.Id, _connectedCircuits.Count);
        
        return Task.CompletedTask;
    }

    // Called when a circuit is disconnected
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Mark the circuit as 'false' to indicate disconnection
        _connectedCircuits[circuit.Id] = false;
        OnUserCountChanged?.Invoke(); // Trigger event

        
        // Log the disconnection
        _logger.LogInformation("Circuit {CircuitId} down. Total connections: {TotalConnections}", 
            circuit.Id, _connectedCircuits.Count);
        
        return Task.CompletedTask;
    }

    // Called when a circuit is permanently disconnected (e.g., browser closed)
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Remove the circuit from the dictionary
        _connectedCircuits.TryRemove(circuit.Id, out _);
        OnUserCountChanged?.Invoke(); // Trigger event

        // Log the circuit closure
        _logger.LogInformation("Circuit {CircuitId} closed. Total connections: {TotalConnections}", 
            circuit.Id, _connectedCircuits.Count);
        
        return Task.CompletedTask;
    }

    // Optional: expose a method to get the current number of connected users
    public int GetTotalConnectedUsers()
    {
        return _connectedCircuits.Count(entry => entry.Value == true);
    }
}
