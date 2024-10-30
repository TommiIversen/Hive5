using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Engine.Utils;

public class CpuUsageMonitor : IDisposable
{
    private bool _disposed;
    private DateTime _lastMeasurementTime = DateTime.UtcNow;
    private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private PerformanceCounter[]? _perCoreCpuCounters;

    private PerformanceCounter? _totalCpuCounter;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    public async Task<double> GetTotalCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return await GetWindowsTotalCpuUsageAsync(cancellationToken);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return await GetLinuxTotalCpuUsageAsync(cancellationToken);

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    public async Task<double[]> GetPerCoreCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return await GetWindowsPerCoreCpuUsageAsync(cancellationToken);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return await GetLinuxPerCoreCpuUsageAsync(cancellationToken);

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    public double GetCurrentProcessCpuUsage()
    {
        var process = Process.GetCurrentProcess();

        // Hent CPU-tid for processen og den aktuelle tid
        var currentTotalProcessorTime = process.TotalProcessorTime;
        var currentMeasurementTime = DateTime.UtcNow;

        // Beregn forskel i CPU-tid og tid mellem to målinger
        var cpuTimeElapsed = currentTotalProcessorTime - _lastTotalProcessorTime;
        var timeElapsed = currentMeasurementTime - _lastMeasurementTime;

        // Opdater de sidste målinger for fremtidige beregninger
        _lastTotalProcessorTime = currentTotalProcessorTime;
        _lastMeasurementTime = currentMeasurementTime;

        // Hvis ingen tid er gået siden sidste måling, returner 0
        if (timeElapsed.TotalMilliseconds == 0)
            return 0;

        // Beregn CPU-forbrug som en procentdel over det forløbne tidsinterval
        var cpuUsage =
            cpuTimeElapsed.TotalMilliseconds / (timeElapsed.TotalMilliseconds * Environment.ProcessorCount) * 100;

        return cpuUsage;
    }

    [SupportedOSPlatform("windows")]
    private Task<double> GetWindowsTotalCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (_totalCpuCounter == null)
        {
            _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _totalCpuCounter.NextValue();
        }

        return Task.Run(() =>
        {
            Thread.Sleep(1000);
            return (double) _totalCpuCounter.NextValue();
        }, cancellationToken);
    }

    [SupportedOSPlatform("windows")]
    private Task<double[]> GetWindowsPerCoreCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (_perCoreCpuCounters == null)
        {
            var coreCount = Environment.ProcessorCount;
            _perCoreCpuCounters = new PerformanceCounter[coreCount];

            for (var i = 0; i < coreCount; i++)
            {
                _perCoreCpuCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                _perCoreCpuCounters[i].NextValue();
            }
        }

        return Task.Run(() =>
        {
            Thread.Sleep(1000);

            var cpuUsages = new double[_perCoreCpuCounters.Length];
            for (var i = 0; i < _perCoreCpuCounters.Length; i++) cpuUsages[i] = _perCoreCpuCounters[i].NextValue();

            return cpuUsages;
        }, cancellationToken);
    }

    // Linux-specific CPU usage retrieval
    private async Task<double> GetLinuxTotalCpuUsageAsync(CancellationToken cancellationToken)
    {
        var cpuLine = await ReadProcStatLineAsync(cancellationToken);

        var cpuValues = ParseCpuStat(cpuLine ?? string.Empty);
        return CalculateCpuUsage(cpuValues);
    }

    private async Task<double[]> GetLinuxPerCoreCpuUsageAsync(CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync("/proc/stat", cancellationToken);
        var coreLines =
            lines.Where(line => line.StartsWith("cpu") && line.Length > 3).ToArray(); // "cpu0", "cpu1", etc.

        var usages = new double[coreLines.Length];
        for (var i = 0; i < coreLines.Length; i++)
        {
            var coreValues = ParseCpuStat(coreLines[i]);
            usages[i] = CalculateCpuUsage(coreValues);
        }

        return usages;
    }

    private async Task<string?> ReadProcStatLineAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader("/proc/stat");
        return await reader.ReadLineAsync(cancellationToken);
    }

    private long[] ParseCpuStat(string cpuStatLine)
    {
        var cpuStats = cpuStatLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new[]
        {
            long.Parse(cpuStats[1]), // user
            long.Parse(cpuStats[2]), // nice
            long.Parse(cpuStats[3]), // system
            long.Parse(cpuStats[4]), // idle
            long.Parse(cpuStats[5]), // iowait
            long.Parse(cpuStats[6]), // irq
            long.Parse(cpuStats[7]) // softirq
        };
    }

    private double CalculateCpuUsage(long[] cpuValues)
    {
        var idleTime = cpuValues[3];
        var totalTime = cpuValues.Sum();

        return 100.0 * (totalTime - idleTime) / totalTime;
    }


    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _totalCpuCounter?.Dispose();

                if (_perCoreCpuCounters != null)
                    foreach (var counter in _perCoreCpuCounters)
                        counter?.Dispose();
            }

            _disposed = true;
        }
    }
}