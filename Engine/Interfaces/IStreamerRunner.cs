using Common.Models;
using Engine.Utils;

namespace Engine.Interfaces;

public interface IStreamerRunner
{
    string WorkerId { get; set; }


    Task<(StreamerState, string)> StartAsync();
    Task<(StreamerState, string)> StopAsync();
    event EventHandler<LogEntry> LogGenerated;
    event EventHandler<ImageData> ImageGenerated;
    StreamerState GetState();
}