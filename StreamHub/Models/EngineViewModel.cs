using Common.Models;

namespace StreamHub.Models;

public class EngineViewModel
{
    public required EngineBaseInfo BaseInfo { get; init; }
    public string? ConnectionId { get; set; }
    public Metric? LastMetric { get; set; }
    public Dictionary<Guid, WorkerViewModel> Workers { get; set; } = new();
    
    public void AddWorkerLog(Guid workerId, string message)
    {
        if (!Workers.ContainsKey(workerId))
        {
            Workers[workerId] = new WorkerViewModel { WorkerId = workerId };
        }
        Workers[workerId].AddLogMessage(message);
    }
}