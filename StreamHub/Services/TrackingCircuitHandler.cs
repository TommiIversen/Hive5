﻿namespace StreamHub.Services;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
  public class UserCountChangedEventArgs(int userCount) : EventArgs
  {
        public int UserCount { get; } = userCount;
    }

    public class TrackingCircuitHandler : CircuitHandler
    {
        private readonly ILogger<TrackingCircuitHandler> _logger;

        // Thread-safe dictionary to keep track of user connections
        private readonly ConcurrentDictionary<string, bool> _connectedCircuits = new();
        
        // Event with data (user count)
        public event EventHandler<UserCountChangedEventArgs>? OnUserCountChanged;

        public TrackingCircuitHandler(ILogger<TrackingCircuitHandler> logger)
        {
            _logger = logger;
        }
        
        public int GetTotalConnectedUsers()
        {
            return _connectedCircuits.Count(entry => entry.Value == true);
        }

        // Called when a circuit is connected
        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _connectedCircuits[circuit.Id] = true;
            RaiseUserCountChangedEvent();
            
            _logger.LogInformation("Circuit {CircuitId} connected. Total connections: {TotalConnections}", 
                circuit.Id, _connectedCircuits.Count);
            return Task.CompletedTask;
        }

        // Called when a circuit is disconnected
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _connectedCircuits[circuit.Id] = false;
            RaiseUserCountChangedEvent();
            
            _logger.LogInformation("Circuit {CircuitId} down. Total connections: {TotalConnections}", 
                circuit.Id, _connectedCircuits.Count);
            return Task.CompletedTask;
        }

        // Called when a circuit is permanently disconnected
        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _connectedCircuits.TryRemove(circuit.Id, out _);
            RaiseUserCountChangedEvent();
            
            _logger.LogInformation("Circuit {CircuitId} closed. Total connections: {TotalConnections}", 
                circuit.Id, _connectedCircuits.Count);
            return Task.CompletedTask;
        }

        // Udløser event med brugerdata (antal tilsluttede brugere)
        private void RaiseUserCountChangedEvent()
        {
            int connectedUsers = _connectedCircuits.Count(entry => entry.Value == true);
            OnUserCountChanged?.Invoke(this, new UserCountChangedEventArgs(connectedUsers));
        }
    }