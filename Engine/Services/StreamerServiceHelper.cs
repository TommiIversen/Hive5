using System.Reflection;
using Engine.Attributes;
using Engine.Interfaces;

namespace Engine.Services;

public static class StreamerServiceHelper
{
    private static readonly Lazy<List<string>> StreamerNames = new(GetStreamerNames);

    public static List<string> GetStreamerNamesCached()
    {
        return StreamerNames.Value;
    }

    private static List<string> GetStreamerNames()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IStreamerService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t =>
                t.GetCustomAttribute<FriendlyNameAttribute>()?.Name ?? t.Name) // Use friendly name if available
            .ToList();
    }
}