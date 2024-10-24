using Common.DTOs;
namespace Engine.Utils;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class SystemInfoCollector
{
    public SystemInfoModel GetSystemInfo()
    {
        return new SystemInfoModel
        {
            OsName = RuntimeInformation.OSDescription,
            OSVersion = Environment.OSVersion.ToString(),
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Uptime = GetSystemUptime(),
            ProcessCount = Process.GetProcesses().Length,
            Platform = GetPlatform()
        };
    }

    private double GetSystemUptime()
    {
        return TimeSpan.FromMilliseconds(Environment.TickCount64).TotalSeconds;
    }

    private string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";

        return "Unknown";
    }
}