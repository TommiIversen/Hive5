using Common.Models;
using Engine.Interfaces;
using Engine.Utils;
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
        IStreamerRunner streamerRunner = new FakeStreamerRunner();
        
        var worker = new WorkerService(_messageQueue, streamerRunner);

        var workerOut = new WorkerOut
        {
            WorkerId = worker.WorkerId,
            Name = workerCreate.Name,
            Description = workerCreate.Description,
            Command = workerCreate.Command,
            Enabled = true,
            IsRunning = false,
        };
        _workers[worker.WorkerId] = worker;
        _workersBaseInfo[worker.WorkerId] = workerOut;
        return worker;
    }


    public CommandResult StartWorker(Guid workerId)
    {
        Log.Information($"Starting worker: {workerId}");
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.Start();
            SendWorkerEvent(workerId, WorkerEventType.Updated);
            return new CommandResult(true, "Worker started");
        }
        return new CommandResult(false, "Worker not found");
    }

    public CommandResult StopWorker(Guid workerId)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.Stop();
            SendWorkerEvent(workerId, WorkerEventType.Updated);
            return new CommandResult(true, "Worker stopped");
        }

        return new CommandResult(false, "Worker not found");
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
    
    private void SendWorkerEvent(Guid workerId, WorkerEventType eventType)
    {
        if (_workersBaseInfo.TryGetValue(workerId, out var workerOut))
        {
            // Opdater `IsRunning` status baseret på `WorkerService`
            if (_workers.TryGetValue(workerId, out var workerService))
            {
                workerOut.IsRunning = workerService.IsRunning;
            }

            var workerEvent = workerOut.ToWorkerEvent(eventType);
            _messageQueue.EnqueueMessage(workerEvent);
        }
        else
        {
            Log.Warning($"WorkerOut not found for WorkerId: {workerId}");
        }
    }
    
}

