namespace Common.DTOs;

public class Metric : BaseMessage
{
    // CPU-målinger
    public required double CPUUsage { get; set; } // Samlet CPU-brug
    public required List<double> PerCoreCpuUsage { get; set; } = [];
    public required double CurrentProcessCpuUsage { get; set; }

    // RAM-målinger
    public required double MemoryUsage { get; set; } // Samlet RAM-brug
    public required double TotalMemory { get; set; } // Samlet mængde RAM i systemet
    public required double AvailableMemory { get; set; } // Tilgængelig RAM i systemet
    public required double CurrentProcessMemoryUsage { get; set; } // RAM-forbrug for den aktuelle proces

    public required double RxMbps { get; set; } // Tilføjet netværksbrug RX
    public required double TxMbps { get; set; } // Tilføjet netværksbrug TX
    public required double RxUsagePercent { get; set; } // Tilføjet netværksbrug RX i procent
    public required double TxUsagePercent { get; set; } // Tilføjet netværksbrug TX i procent
    public required string NetworkInterfaceName { get; set; } // Tilføjet netværksinterface-navn
    public required double LinkSpeedGbps { get; set; } // Tilføjet netværksinterface hastighed
}