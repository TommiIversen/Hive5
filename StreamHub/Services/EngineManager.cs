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
        if (!_engines.TryGetValue(engineId, out var engineInfo)) return null;
        engineInfo.Workers.TryGetValue(workerId, out var worker);
        return worker;
    }

    public IEnumerable<EngineViewModel> GetAllEngines()
    {
        return _engines.Values;
    }
}