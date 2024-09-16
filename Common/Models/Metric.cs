// Models/Metric.cs
namespace Common.Models;

public class Metric
{
    public Guid EngineId { get; set; }
    public required DateTime Timestamp { get; set; }
    
    // CPU-målinger
    public required double CPUUsage { get; set; }  // Samlet CPU-brug
    public required List<double> PerCoreCpuUsage { get; set; } = new();
    public required double CurrentProcessCpuUsage { get; set; }

    // RAM-målinger
    public required double MemoryUsage { get; set; }  // Samlet RAM-brug
    public required double TotalMemory { get; set; }  // Samlet mængde RAM i systemet
    public required double AvailableMemory { get; set; }  // Tilgængelig RAM i systemet
    public required double CurrentProcessMemoryUsage { get; set; }  // RAM-forbrug for den aktuelle proces
}
