using System.Collections.Concurrent;
using Common.Models;
using StreamHub.Models;

namespace StreamHub.Services;

public class EngineManager
{
    private readonly ConcurrentDictionary<Guid, EngineViewModel> _engines = new();

    public EngineViewModel GetOrAddEngine(Guid engineId)
    {
        return _engines.GetOrAdd(engineId, id => new EngineViewModel { EngineId = id });
    }

    public bool TryGetEngine(Guid engineId, out EngineViewModel engineInfo)
    {
        return _engines.TryGetValue(engineId, out engineInfo);
    }
    

    public void RemoveConnection(string connectionId)
    {
        var engine = _engines.Values.FirstOrDefault(e => e.ConnectionId == connectionId);
        if (engine != null)
        {
            engine.ConnectionId = null;
        }
    }
    
    public WorkerViewModel? GetWorker(Guid engineId, Guid workerId)
    {
        if (_engines.TryGetValue(engineId, out var engineInfo))
        {
            engineInfo.Workers.TryGetValue(workerId, out var worker);
            return worker;
        }
        return null;
    }

    public IEnumerable<EngineViewModel> GetAllEngines()
    {
        return _engines.Values;
    }
    

}

