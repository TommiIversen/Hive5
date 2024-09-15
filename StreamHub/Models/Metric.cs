// Models/Metric.cs
namespace StreamHub.Models;

public class Metric
{
    public Guid EngineId { get; set; }
    public DateTime Timestamp { get; set; }
    public double CPUUsage { get; set; }
    public double MemoryUsage { get; set; }
}