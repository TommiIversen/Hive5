using System.Collections.Concurrent;
using Common.DTOs;
using Common.DTOs.Events;
using StreamHub.Models;

namespace StreamHub.Services;

public class EngineManager
{
    private readonly ConcurrentDictionary<Guid, EngineViewModel> _engines = new();

    public EngineViewModel GetOrAddEngine(EngineBaseInfo baseInfo)
    {
        return _engines.GetOrAdd(baseInfo.EngineId, _ => new EngineViewModel {BaseInfo = baseInfo});
    }

    // method to update base info
    public void UpdateBaseInfo(EngineBaseInfo baseInfo)
    {
        Console.WriteLine($"Updating base info for engine {baseInfo.EngineId}");
        if (_engines.TryGetValue(baseInfo.EngineId, out var engine))
        {
            Console.WriteLine($"Base info for engine {baseInfo.EngineId} updated.");
            engine.BaseInfo = baseInfo;
            engine.BaseInfo.HubUrls = new List<HubUrlInfo>(baseInfo.HubUrls); // Kopi af ny HubUrls-liste

            // print out urls
            foreach (var url in engine.BaseInfo.HubUrls) Console.WriteLine($"UPDATEEEEE HubUrl: {url.HubUrl}");
        }
    }

    public void AddOrUpdateWorker(WorkerInfo workerInfo)
    {
        if (_engines.TryGetValue(workerInfo.EngineId, out var engine))
        {
            if (engine.Workers.TryGetValue(workerInfo.WorkerId, out var workerViewModel))
            {
                if (IsOutdatedEvent(workerViewModel, workerInfo))
                {
                    Console.WriteLine($"Skipping outdated event for worker {workerInfo.WorkerId} - {workerInfo.Name}");
                    return;
                }

                UpdateExistingWorker(workerViewModel, workerInfo);
            }
            else
            {
                AddNewWorker(engine, workerInfo);
            }
        }
        else
        {
            Console.WriteLine($"Engine {workerInfo.EngineId} not found. Cannot add or update worker.");
        }
    }

    private bool IsOutdatedEvent(WorkerViewModel workerViewModel, WorkerInfo workerInfo)
    {
        return workerViewModel.EventProcessedTimestamp >= workerInfo.Timestamp;
    }

    private void UpdateExistingWorker(WorkerViewModel workerViewModel, WorkerInfo workerInfo)
    {
        workerViewModel.Worker = workerInfo;
        workerViewModel.EventProcessedTimestamp = workerInfo.Timestamp; // Opdater tidsstemplet
        Console.WriteLine($"Worker {workerInfo.WorkerId} updated.");
    }

    private void AddNewWorker(EngineViewModel engine, WorkerInfo workerInfo)
    {
        engine.Workers[workerInfo.WorkerId] = new WorkerViewModel
        {
            WorkerId = workerInfo.WorkerId,
            Worker = workerInfo,
            EventProcessedTimestamp = workerInfo.Timestamp
        };
        Console.WriteLine($"Worker {workerInfo.WorkerId} added.");
    }


    public bool TryGetEngine(Guid engineId, out EngineViewModel? engineInfo)
    {
        return _engines.TryGetValue(engineId, out engineInfo);
    }


    public bool RemoveConnection(string connectionId)
    {
        var engine = _engines.Values.FirstOrDefault(e => e.ConnectionInfo.ConnectionId == connectionId);
        if (engine == null) return false;

        engine.ConnectionInfo.ConnectionId = "";
        return true;
    }

    public WorkerViewModel? GetWorker(Guid engineId, string workerId)
    {
        if (!_engines.TryGetValue(engineId, out var engineInfo)) return null;
        engineInfo.Workers.TryGetValue(workerId, out var worker);
        return worker;
    }

    public IEnumerable<EngineViewModel> GetAllEngines()
    {
        return _engines.Values;
    }

    public bool RemoveEngine(Guid engineId)
    {
        return _engines.TryRemove(engineId, out _);
    }

    public void SynchronizeWorkers(List<WorkerChangeEvent> workers, Guid engineId)
    {
        Console.WriteLine($"-----------SynchronizeWorkers workers: {workers.Count}");

        // Få engine fra EngineManager
        if (!_engines.TryGetValue(engineId, out var engine))
        {
            Console.WriteLine($"Engine {engineId} not found, cannot synchronize workers.");
            return;
        }

        // Liste over eksisterende workers i hukommelsen for denne engine
        var existingWorkers = engine.Workers.Keys.ToList();

        // Opdater eksisterende workers og tilføj nye
        foreach (var worker in workers)
        {
            AddOrUpdateWorker(worker);
            Console.WriteLine($"Added/Updated Worker: {worker.Name} {worker.IsEnabled}");

            // Fjern denne workerId fra listen over eksisterende workers
            existingWorkers.Remove(worker.WorkerId);
        }

        // Fjern workers, som er i hukommelsen, men ikke længere findes på den modtagne liste
        foreach (var workerId in existingWorkers)
        {
            Console.WriteLine($"Removing outdated Worker: {workerId}");
            RemoveWorker(engineId, workerId);
        }

        Console.WriteLine("Workers synchronized successfully.");
    }

    public void RemoveWorker(Guid engineId, string workerId)
    {
        if (_engines.TryGetValue(engineId, out var engine))
        {
            if (engine.Workers.ContainsKey(workerId))
            {
                engine.Workers.Remove(workerId, out _);
                Console.WriteLine($"Worker {workerId} successfully removed from engine {engineId}.");
            }
            else
            {
                Console.WriteLine($"Worker {workerId} not found in engine {engineId}. Cannot remove.");
            }
        }
        else
        {
            Console.WriteLine($"Engine {engineId} not found. Cannot remove worker {workerId}.");
        }
    }
}