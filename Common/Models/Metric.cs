// Models/Metric.cs
namespace StreamHub.Models;

public class Metric
{
    public Guid EngineId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required double CPUUsage { get; set; }  // Samlet CPU-brug
    public required double MemoryUsage { get; set; }  // Samlet RAM-brug

    // Ny liste til CPU-brug for hver kerne
    public required List<double> PerCoreCpuUsage { get; set; } = new();

    // Ny værdi til CPU-brug for den aktuelle proces
    public required double CurrentProcessCpuUsage { get; set; }

    // Nye felter til RAM-målinger
    public required double TotalMemory { get; set; }  // Samlet mængde RAM i systemet
    public required double AvailableMemory { get; set; }  // Tilgængelig RAM i systemet
    public required double CurrentProcessMemoryUsage { get; set; }  // RAM-forbrug for den aktuelle proces
}
