using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Engine.Utils;

public class CpuUsageMonitor
{
    private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private DateTime _lastMeasurementTime = DateTime.UtcNow;
    
    public async Task<double> GetTotalCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await GetWindowsTotalCpuUsageAsync(cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return await GetLinuxTotalCpuUsageAsync(cancellationToken);
        }

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    public async Task<double[]> GetPerCoreCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await GetWindowsPerCoreCpuUsageAsync(cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return await GetLinuxPerCoreCpuUsageAsync(cancellationToken);
        }

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    public double GetCurrentProcessCpuUsage()
    {
        Process process = Process.GetCurrentProcess();

        // Hent CPU-tid for processen og den aktuelle tid
        TimeSpan currentTotalProcessorTime = process.TotalProcessorTime;
        DateTime currentMeasurementTime = DateTime.UtcNow;

        // Beregn forskel i CPU-tid og tid mellem to målinger
        TimeSpan cpuTimeElapsed = currentTotalProcessorTime - _lastTotalProcessorTime;
        TimeSpan timeElapsed = currentMeasurementTime - _lastMeasurementTime;

        // Opdater de sidste målinger for fremtidige beregninger
        _lastTotalProcessorTime = currentTotalProcessorTime;
        _lastMeasurementTime = currentMeasurementTime;

        // Hvis ingen tid er gået siden sidste måling, returner 0
        if (timeElapsed.TotalMilliseconds == 0)
            return 0;

        // Beregn CPU-forbrug som en procentdel over det forløbne tidsinterval
        double cpuUsage = (cpuTimeElapsed.TotalMilliseconds / (timeElapsed.TotalMilliseconds * Environment.ProcessorCount)) * 100;

        return cpuUsage;
    }

    [SupportedOSPlatform("windows")]
    private Task<double> GetWindowsTotalCpuUsageAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            Thread.Sleep(1000); // CPU-usage data needs a bit of delay for accuracy
            return (double)cpuCounter.NextValue();
        }, cancellationToken);
    }
    
    [SupportedOSPlatform("windows")]
    private Task<double[]> GetWindowsPerCoreCpuUsageAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            int coreCount = Environment.ProcessorCount;
            var cpuCounters = new PerformanceCounter[coreCount];

            for (int i = 0; i < coreCount; i++)
            {
                cpuCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                cpuCounters[i].NextValue();
            }

            Thread.Sleep(1000); // CPU-usage data needs a bit of delay for accuracy

            double[] cpuUsages = new double[coreCount];
            for (int i = 0; i < coreCount; i++)
            {
                cpuUsages[i] = cpuCounters[i].NextValue();
            }

            return cpuUsages;
        }, cancellationToken);
    }

    // Linux-specific CPU usage retrieval
    private async Task<double> GetLinuxTotalCpuUsageAsync(CancellationToken cancellationToken)
    {
        var cpuLine = await ReadProcStatLineAsync(cancellationToken);
        var cpuValues = ParseCpuStat(cpuLine);
        return CalculateCpuUsage(cpuValues);
    }

    private async Task<double[]> GetLinuxPerCoreCpuUsageAsync(CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync("/proc/stat", cancellationToken);
        var coreLines = lines.Where(line => line.StartsWith("cpu") && line.Length > 3).ToArray(); // "cpu0", "cpu1", etc.

        double[] usages = new double[coreLines.Length];
        for (int i = 0; i < coreLines.Length; i++)
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
        return new long[]
        {
            long.Parse(cpuStats[1]), // user
            long.Parse(cpuStats[2]), // nice
            long.Parse(cpuStats[3]), // system
            long.Parse(cpuStats[4]), // idle
            long.Parse(cpuStats[5]), // iowait
            long.Parse(cpuStats[6]), // irq
            long.Parse(cpuStats[7]), // softirq
        };
    }

    private double CalculateCpuUsage(long[] cpuValues)
    {
        long idleTime = cpuValues[3];
        long totalTime = cpuValues.Sum();

        return 100.0 * (totalTime - idleTime) / totalTime;
    }
}