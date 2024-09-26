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
                // Update the existing worker
                workerViewModel.Worker = workerOut;
            }
            else
            {
                engine.Workers[workerOut.WorkerId] = new WorkerViewModel
                {
                    WorkerId = workerOut.WorkerId,
                    Worker = workerOut
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

    public WorkerViewModel? GetWorker(Guid engineId, Guid workerId)
    {
        if (!_engines.TryGetValue(engineId, out var engineInfo)) return null;
        engineInfo.Workers.TryGetValue(workerId, out var worker);
        return worker;
    }

    public IEnumerable<EngineViewModel> GetAllEngines()
    {
        return _engines.Values;
    }
}