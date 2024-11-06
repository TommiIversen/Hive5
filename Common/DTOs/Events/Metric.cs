namespace Common.DTOs.Events;

public class Metric : BaseMessage
{
    // CPU-målinger
    public required double CpuUsage { get; init; } // Samlet CPU-brug
    public required List<double> PerCoreCpuUsage { get; init; } = [];
    public required double CurrentProcessCpuUsage { get; init; }

    // RAM-målinger
    public required double MemoryUsage { get; init; } // Samlet RAM-brug
    public required double TotalMemory { get; init; } // Samlet mængde RAM i systemet
    public required double AvailableMemory { get; init; } // Tilgængelig RAM i systemet
    public required double CurrentProcessMemoryUsage { get; init; } // RAM-forbrug for den aktuelle proces

    public required double RxMbps { get; init; } // Tilføjet netværksbrug RX
    public required double TxMbps { get; init; } // Tilføjet netværksbrug TX
    public required double RxUsagePercent { get; init; } // Tilføjet netværksbrug RX i procent
    public required double TxUsagePercent { get; init; } // Tilføjet netværksbrug TX i procent
    public required string NetworkInterfaceName { get; init; } // Tilføjet netværksinterface-navn
    public required double LinkSpeedGbps { get; init; } // Tilføjet netværksinterface hastighed
    public required DateTime MeasureTimestamp { get; init; }
}