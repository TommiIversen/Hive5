using Common.DTOs;
using Engine.Interfaces;
using Engine.Utils;

namespace Engine.Services;

public class MetricsService : IHostedService, IDisposable
{
    private readonly IMessageQueue _messageQueue;
    private readonly INetworkInterfaceProvider _networkInterfaceProvider;
    private readonly CpuUsageMonitor _cpuUsageMonitor = new();
    private readonly MemoryUsageMonitor _memoryUsageMonitor = new();
    private readonly NetworkUsageMonitor _networkUsageMonitor;
    private readonly TimeSpan _interval;
    private Timer? _timer;

    public MetricsService(
        IMessageQueue messageQueue,
        INetworkInterfaceProvider networkInterfaceProvider,
        TimeSpan? interval = null)
    {
        _messageQueue = messageQueue;
        _networkInterfaceProvider = networkInterfaceProvider;
        _networkUsageMonitor = new NetworkUsageMonitor(networkInterfaceProvider);
        _interval = interval ?? TimeSpan.FromSeconds(2);
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Metrics generation task started.");
        _timer = new Timer(async _ => await GenerateMetricsAsync(cancellationToken), null, TimeSpan.Zero, _interval);
        return Task.CompletedTask;
    }

    public async Task GenerateMetricsAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // Hent CPU- og RAM-målinger
        var totalCpuUsage = await GetCpuUsageAsync(cancellationToken);
        var perCoreCpuUsage = await GetPerCoreCpuUsageAsync(cancellationToken);
        var currentProcessCpuUsage = _cpuUsageMonitor.GetCurrentProcessCpuUsage();

        var totalMemory = await _memoryUsageMonitor.GetTotalMemoryAsync(cancellationToken);
        var availableMemory = await _memoryUsageMonitor.GetAvailableMemoryAsync(cancellationToken);
        var currentProcessMemoryUsage = _memoryUsageMonitor.GetCurrentProcessMemoryUsage();

        var networkUsageList = _networkUsageMonitor.GetNetworkUsage();
        var primaryNetworkUsage = networkUsageList.FirstOrDefault();

        double rxMbps = 0, txMbps = 0, rxUsagePercent = 0, txUsagePercent = 0;
        var interfaceName = "";
        double linkSpeedGbps = 0;

        if (primaryNetworkUsage != null)
        {
            rxMbps = primaryNetworkUsage.RxMbps;
            txMbps = primaryNetworkUsage.TxMbps;
            rxUsagePercent = primaryNetworkUsage.RxUsagePercent;
            txUsagePercent = primaryNetworkUsage.TxUsagePercent;
            interfaceName = primaryNetworkUsage.InterfaceName;
            linkSpeedGbps = primaryNetworkUsage.LinkSpeedGbps;
        }

        var metric = new Metric
        {
            Timestamp = DateTime.UtcNow,
            CPUUsage = totalCpuUsage,
            PerCoreCpuUsage = perCoreCpuUsage,
            CurrentProcessCpuUsage = currentProcessCpuUsage,
            MemoryUsage = totalMemory - availableMemory,
            TotalMemory = totalMemory,
            AvailableMemory = availableMemory,
            CurrentProcessMemoryUsage = currentProcessMemoryUsage,
            RxMbps = rxMbps,
            TxMbps = txMbps,
            RxUsagePercent = rxUsagePercent,
            TxUsagePercent = txUsagePercent,
            NetworkInterfaceName = interfaceName,
            LinkSpeedGbps = linkSpeedGbps,
            MeasureTimestamp = DateTime.UtcNow
        };

        await _messageQueue.EnqueueMessageAsync(metric, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Metrics generation task stopped.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private async Task<double> GetCpuUsageAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Brug CpuUsageMonitor til at hente samlet CPU-forbrug
            return await _cpuUsageMonitor.GetTotalCpuUsageAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error measuring CPU usage: {ex.Message}");
            return 0.0; // Returner 0 i tilfælde af fejl
        }
    }

    private async Task<List<double>> GetPerCoreCpuUsageAsync(CancellationToken cancellationToken)
    {
        try
        {
            var perCoreUsage = await _cpuUsageMonitor.GetPerCoreCpuUsageAsync(cancellationToken);
            return perCoreUsage.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error measuring per core CPU usage: {ex.Message}");
            return new List<double>(); // Returner tom liste i tilfælde af fejl
        }
    }
}