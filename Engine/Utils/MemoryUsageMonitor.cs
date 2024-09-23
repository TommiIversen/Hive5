using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Engine.Utils;

public class MemoryUsageMonitor
{
    // Struct til at holde hukommelsesstatus fra Windows API
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public async Task<double> GetTotalMemoryAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await GetWindowsTotalMemoryAsync(cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return await GetLinuxTotalMemoryAsync(cancellationToken);
        }

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    public async Task<double> GetAvailableMemoryAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await GetWindowsAvailableMemoryAsync(cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return await GetLinuxAvailableMemoryAsync(cancellationToken);
        }

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    public double GetCurrentProcessMemoryUsage()
    {
        Process process = Process.GetCurrentProcess();
        return process.WorkingSet64 / (1024.0 * 1024.0); // Returnerer i MB
    }

    // Windows-specific memory retrieval using GlobalMemoryStatusEx
    private Task<double> GetWindowsTotalMemoryAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                // Returner samlet hukommelse i MB
                return memStatus.ullTotalPhys / (1024.0 * 1024.0);
            }
            throw new InvalidOperationException("Unable to get total memory");
        }, cancellationToken);
    }

    private Task<double> GetWindowsAvailableMemoryAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                // Returner tilgængelig hukommelse i MB
                return memStatus.ullAvailPhys / (1024.0 * 1024.0);
            }
            throw new InvalidOperationException("Unable to get available memory");
        }, cancellationToken);
    }

    // Linux-specific memory retrieval
    private async Task<double> GetLinuxTotalMemoryAsync(CancellationToken cancellationToken)
    {
        var memInfo = await File.ReadAllLinesAsync("/proc/meminfo", cancellationToken);
        var totalMemoryLine = Array.Find(memInfo, line => line.StartsWith("MemTotal"));
        return ParseMemoryValue(totalMemoryLine) / 1024.0; // Returner MB
    }

    private async Task<double> GetLinuxAvailableMemoryAsync(CancellationToken cancellationToken)
    {
        var memInfo = await File.ReadAllLinesAsync("/proc/meminfo", cancellationToken);
        var availableMemoryLine = Array.Find(memInfo, line => line.StartsWith("MemAvailable"));
        if (availableMemoryLine != null)
            return ParseMemoryValue(availableMemoryLine) / 1024.0; // Returner MB
        else
            return 0.0;
    }

    // Helper method to parse memory values in /proc/meminfo on Linux
    private double ParseMemoryValue(string memInfoLine)
    {
        var parts = memInfoLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return double.Parse(parts[1]); // Memory is in KB in /proc/meminfo
    }
}