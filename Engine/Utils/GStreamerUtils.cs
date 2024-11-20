using System.Runtime.InteropServices;

namespace Engine.Utils;

public static class GStreamerUtils
{
    public static string? FindGStreamerExecutable()
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "gst-launch-1.0.exe"
            : "gst-launch-1.0";

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (pathVariable == null)
        {
            Console.WriteLine("Advarsel: PATH-miljøvariablen er ikke sat.");
            return null;
        }

        string[] paths = pathVariable.Split(Path.PathSeparator);

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, executableName);
            if (File.Exists(fullPath)) return fullPath;
        }

        Console.WriteLine($"Advarsel: GStreamer-eksekverbar '{executableName}' blev ikke fundet i PATH.");
        return null;
    }
}