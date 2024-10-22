using System.Collections.Concurrent;
using Common.Models;
using StreamHub.Models;

namespace StreamHub.Services;

public class EngineManager
{
    private readonly ConcurrentDictionary<Guid, EngineViewModel> _engines = new();

    public EngineViewModel GetOrAddEngine(EngineBaseInfo baseInfo)
    {
        return _engines.GetOrAdd(baseInfo.EngineId, id => new EngineViewModel { BaseInfo = baseInfo });
    }

    public void AddOrUpdateWorker(WorkerOut workerOut)
    {
        if (_engines.TryGetValue(workerOut.EngineId, out var engine))
        {
            if (engine.Workers.TryGetValue(workerOut.WorkerId, out var workerViewModel))
            {
                if (workerViewModel.EventProcessedTimestamp >= workerOut.Timestamp)
                {
                    // Anvender Message Filter til at sammenligne indkommende beskeds timestamp 
                    // med den sidst behandlede event for at implementere en Idempotent Receiver, 
                    // som forhindrer behandling af forældede eller duplikerede beskeder.
                    Console.WriteLine($"Skipping outdated event for worker {workerOut.WorkerId} - {workerOut.Name}");
                    Console.WriteLine(
                        $"EventProcessedTimestamp: {workerViewModel.EventProcessedTimestamp} VS workerOut.Timestamp:  {workerOut.Timestamp}");
                    return; // Hvis eventet er ældre eller lig med den nuværende tilstand, gør ingenting
                }

                // Update the existing worker
                workerViewModel.Worker = workerOut;
                workerViewModel.EventProcessedTimestamp = workerOut.Timestamp; // Opdater tidsstemplet
            }
            else
            {
                engine.Workers[workerOut.WorkerId] = new WorkerViewModel
                {
                    WorkerId = workerOut.WorkerId,
                    Worker = workerOut,
                    EventProcessedTimestamp = workerOut.Timestamp
                };
            }
        }
        else
        {
            // Handle the case where the engine does not exist
            Console.WriteLine($"Engine {workerOut.EngineId} not found. Cannot add or update worker.");
        }
    }


    public bool TryGetEngine(Guid engineId, out EngineViewModel engineInfo)
    {
        return _engines.TryGetValue(engineId, out engineInfo);
    }


    public bool RemoveConnection(string connectionId)
    {
        var engine = _engines.Values.FirstOrDefault(e => e.ConnectionId == connectionId);
        if (engine == null) return false;

        engine.ConnectionId = "";
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
    
    public void SynchronizeWorkers(List<WorkerEvent> workers, Guid engineId)
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