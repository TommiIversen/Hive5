using System.Collections.Concurrent;
using Common.Models;

namespace StreamHub.Models;

public class EngineViewModel
{
    public required EngineBaseInfo BaseInfo { get; set; }
    public string? ConnectionId { get; set; }
    public Metric? LastMetric { get; set; }
    public ConcurrentDictionary<string, WorkerViewModel> Workers { get; } = new();
    
    // New fields
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public string? TransportType { get; set; }
    public DateTime? OnlineSince { get; set; }

    public TimeSpan? Uptime => OnlineSince.HasValue ? DateTime.UtcNow - OnlineSince.Value : null;

    
    public ConcurrentQueue<MetricSimpleViewModel> MetricsQueue { get; set; } = new();
    public int LocalPort { get; set; }


    public bool AddWorkerLog(string workerId, LogEntry message)
    {
        if (!Workers.TryGetValue(workerId, out var worker)) return false;

        worker.AddLogMessage(message);
        return true;

    }
    
    public void AddMetric(Metric metric)
    {
        var simplifiedMetric = new MetricSimpleViewModel(metric);
        MetricsQueue.Enqueue(simplifiedMetric);
        if (MetricsQueue.Count > 20)
        {
            MetricsQueue.TryDequeue(out _);
        }
        LastMetric = metric;
    }
}