using System.Collections.Concurrent;
using Common.Models;

namespace StreamHub.Services;

public class EngineManager
{
    private readonly ConcurrentDictionary<Guid, EngineInfo> _engines = new();

    public EngineInfo GetOrAddEngine(Guid engineId)
    {
        return _engines.GetOrAdd(engineId, id => new EngineInfo { EngineId = id });
    }

    public bool TryGetEngine(Guid engineId, out EngineInfo engineInfo)
    {
        return _engines.TryGetValue(engineId, out engineInfo);
    }

    public void RemoveConnection(string connectionId)
    {
        var engine = _engines.Values.FirstOrDefault(e => e.ConnectionId == connectionId);
        if (engine != null)
        {
            engine.ConnectionId = null; // Clean up connection
        }
    }

    public IEnumerable<EngineInfo> GetAllEngines()
    {
        return _engines.Values;
    }
}

public class EngineInfo
{
    public Guid EngineId { get; set; }
    public string ConnectionId { get; set; }
    public Metric LastMetric { get; set; }
}