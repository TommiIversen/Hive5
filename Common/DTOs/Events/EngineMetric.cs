namespace Common.DTOs.Events;

public class EngineMetric : BaseMessage
{
    // CPU-målinger
    public required double CpuUsage { get; init; }
    public required List<double> PerCoreCpuUsage { get; init; } = [];
    public required double CurrentProcessCpuUsage { get; init; }

    // RAM-målinger
    public required double MemoryUsage { get; init; } // Samlet RAM-brug
    public required double TotalMemory { get; init; } // Samlet mængde RAM i systemet
    public required double AvailableMemory { get; init; } // Tilgængelig RAM i systemet
    public required double CurrentProcessMemoryUsage { get; init; } // RAM-forbrug for den aktuelle proces

    public required double RxMbps { get; init; }
    public required double TxMbps { get; init; }
    public required double RxUsagePercent { get; init; }
    public required double TxUsagePercent { get; init; }
    public required string NetworkInterfaceName { get; init; }
    public required double LinkSpeedGbps { get; init; }
    public required DateTime MeasureTimestamp { get; init; }
}