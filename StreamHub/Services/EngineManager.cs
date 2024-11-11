using System.Collections.Concurrent;
using Common.DTOs;
using Common.DTOs.Events;
using StreamHub.Models;

namespace StreamHub.Services;

public class EngineManager
{
    private readonly ConcurrentDictionary<Guid, EngineViewModel> _engines = new();

    public EngineViewModel GetOrAddEngine(BaseEngineInfo info)
    {
        return _engines.GetOrAdd(info.EngineId, _ => new EngineViewModel { Info = info });
    }

    // method to update base info
    public void UpdateBaseInfo(BaseEngineInfo info)
    {
        Console.WriteLine($"Updating base info for engine {info.EngineId}");
        if (_engines.TryGetValue(info.EngineId, out var engine))
        {
            Console.WriteLine($"Base info for engine {info.EngineId} updated.");
            engine.Info = info;
            engine.Info.HubUrls = new List<HubUrlInfo>(info.HubUrls); // Kopi af ny HubUrls-liste

            // print out urls
            foreach (var url in engine.Info.HubUrls) Console.WriteLine($"UPDATEEEEE HubUrl: {url.HubUrl}");
        }
    }

   
    public void AddOrUpdateWorker(BaseWorkerInfo baseWorkerInfo)
    {
        if (_engines.TryGetValue(baseWorkerInfo.EngineId, out var engine))
        {
            if (engine.Workers.TryGetValue(baseWorkerInfo.WorkerId, out var existingWorker))
            {
                if (IsOutdatedEvent(existingWorker, baseWorkerInfo))
                {
                    Console.WriteLine($"Skipping outdated event for worker {baseWorkerInfo.WorkerId} - {baseWorkerInfo.Name}");
                    return;
                }

                existingWorker.BaseWorker = baseWorkerInfo;
                existingWorker.EventProcessedTimestamp = baseWorkerInfo.Timestamp;
                engine.UpdateWorker(existingWorker);
                Console.WriteLine($"Worker {baseWorkerInfo.WorkerId} updated.");
            }
            else
            {
                var newWorker = new WorkerViewModel
                {
                    WorkerId = baseWorkerInfo.WorkerId,
                    BaseWorker = baseWorkerInfo,
                    EventProcessedTimestamp = baseWorkerInfo.Timestamp
                };
                engine.AddWorker(newWorker);
                Console.WriteLine($"Worker {baseWorkerInfo.WorkerId} added.");
            }
        }
        else
        {
            Console.WriteLine($"Engine {baseWorkerInfo.EngineId} not found. Cannot add or update worker.");
        }
    }

    private bool IsOutdatedEvent(WorkerViewModel workerViewModel, BaseWorkerInfo baseWorkerInfo)
    {
        return workerViewModel.EventProcessedTimestamp >= baseWorkerInfo.Timestamp;
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
            engine.RemoveWorker(workerId);
            Console.WriteLine($"Worker {workerId} successfully removed from engine {engineId}.");
        }
        else
        {
            Console.WriteLine($"Engine {engineId} not found. Cannot remove worker {workerId}.");
        }
    }
}