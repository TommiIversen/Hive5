using Common.Models;

namespace StreamHub.Models;

public class EngineViewModel
{
    public required EngineBaseInfo BaseInfo { get; init; }
    public string? ConnectionId { get; set; }
    public Metric? LastMetric { get; set; }
    public Dictionary<string, WorkerViewModel> Workers { get; set; } = new();
    
    public bool AddWorkerLog(string workerId, LogEntry message)
    {
        if (Workers.ContainsKey(workerId))
        {
            //Workers[workerId] = new WorkerViewModel { WorkerId = workerId };
            Workers[workerId].AddLogMessage(message);
            return true;
        }

        return false;
    }
}