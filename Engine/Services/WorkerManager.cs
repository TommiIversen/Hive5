using Common.Models;
using Serilog;

namespace Engine.Services;

public class WorkerManager
{
    private readonly MessageQueue _messageQueue;
    private readonly Dictionary<Guid, WorkerService> _workers = new();
    
    // gem her til vi får en database
    private readonly Dictionary<Guid, WorkerOut> _workersBaseInfo = new();

    
    public WorkerManager(MessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
    }

    public IReadOnlyDictionary<Guid, WorkerService> Workers => _workers;

    public WorkerService AddWorker(WorkerCreate workerCreate)
    {
        Log.Information($"Adding worker... {workerCreate.Name}");

        // Opret en ny WorkerService-instans
        var worker = new WorkerService(_messageQueue);

        // Opret en ny WorkerOut baseret på WorkerCreate og WorkerService
        var workerOut = new WorkerOut
        {
            WorkerId = worker.WorkerId,  // Brug WorkerId fra WorkerService
            Name = workerCreate.Name,    // Brug data fra WorkerCreate
            Description = workerCreate.Description,
            Command = workerCreate.Command,
            Enabled = true,              // Eksempel: Default til enabled
            IsRunning = false            // Eksempel: Sæt default til ikke kørende
        };

        // Tilføj WorkerService til _workers
        _workers[worker.WorkerId] = worker;

        // Tilføj WorkerOut til _workersBaseInfo
        _workersBaseInfo[worker.WorkerId] = workerOut;

        return worker;
    }


    public void StartWorker(Guid workerId)
    {
        Log.Information($"Starting worker: {workerId}");
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.Start();
        }
    }

    public void StopWorker(Guid workerId)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.Stop();
        }
    }

    public List<WorkerOut> GetAllWorkers(Guid engineId)
    {
        // FIX - inject ikke enginID her
        Log.Information($"Getting all workers...");
        
        foreach (var worker in _workersBaseInfo.Values)
            worker.EngineId = engineId;
        
        return _workersBaseInfo.Values.ToList();
    }

    public void RemoveWorker(Guid workerId)
    {
        _workers.Remove(workerId);
    }
}