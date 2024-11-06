using System.Collections.Concurrent;
using Common.DTOs;
using Common.DTOs.Events;

namespace StreamHub.Models;

public class ConnectionInfo
{
    public string? ConnectionId { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public string? TransportType { get; set; }
    public int LocalPort { get; set; }
    public DateTime? OnlineSince { get; set; }
    public TimeSpan? Uptime => OnlineSince.HasValue ? DateTime.UtcNow - OnlineSince.Value : null;
}

public class EngineViewModel
{
    public required EngineBaseInfo BaseInfo { get; set; }
    public SystemInfoModel? SystemInfo { get; set; }
    public Metric? LastMetric { get; set; }
    public ConcurrentDictionary<string, WorkerViewModel> Workers { get; } = new();
    public ConcurrentQueue<EngineLogEntry> EngineLogMessages { get; set; } = new();
    public ConnectionInfo ConnectionInfo { get; set; } = new();
    public ConcurrentQueue<MetricSimpleViewModel> MetricsQueue { get; set; } = new();

    public bool AddWorkerLog(string workerId, WorkerLogEntry message)
    {
        if (!Workers.TryGetValue(workerId, out var worker)) return false;

        worker.AddLogMessage(message);
        return true;
    }

    public void AddEngineLog(EngineLogEntry message)
    {
        EngineLogMessages.Enqueue(message);
        if (EngineLogMessages.Count > 50) EngineLogMessages.TryDequeue(out _); // Remove the oldest message
    }

    public void ClearEngineLogs()
    {
        EngineLogMessages = new ConcurrentQueue<EngineLogEntry>();
    }

    public void AddMetric(Metric metric)
    {
        var simplifiedMetric = new MetricSimpleViewModel(metric);
        MetricsQueue.Enqueue(simplifiedMetric);
        if (MetricsQueue.Count > 20) MetricsQueue.TryDequeue(out _);
        LastMetric = metric;
    }
}