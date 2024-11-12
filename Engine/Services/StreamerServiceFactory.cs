using System.Reflection;
using Engine.Attributes;
using Engine.Interfaces;

namespace Engine.Services;

public static class StreamerServiceFactory
{
    private static readonly Dictionary<string, Type> _streamerTypes = new();

    static StreamerServiceFactory()
    {
        // Load all types in the current assembly implementing IStreamerService
        var streamerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IStreamerService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in streamerTypes)
        {
            // Check for the FriendlyName attribute
            var friendlyNameAttribute = type.GetCustomAttribute<FriendlyNameAttribute>();
            var key = friendlyNameAttribute?.Name ??
                      type.Name; // Use friendly name if present, otherwise fall back to type name
            _streamerTypes[key] = type;
        }
    }

    public static IStreamerService CreateStreamerService(string typeName, string workerId, string gstCommand)
    {
        if (_streamerTypes.TryGetValue(typeName, out var type))
        {
            var instance = (IStreamerService)Activator.CreateInstance(type);
            instance.WorkerId = workerId;
            instance.GstCommand = gstCommand;
            return instance;
        }

        throw new ArgumentException($"Streamer service type '{typeName}' not found.");
    }
}