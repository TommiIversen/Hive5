// Models/Metric.cs
namespace StreamHub.Models;

public class Metric
{
    public Guid EngineId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required double CPUUsage { get; set; }
    public required double MemoryUsage { get; set; }
}