namespace Engine.Models;

public class StreamHubOptions
{
    public List<string> HubUrls { get; set; } = new();
    public int MaxQueueSize { get; set; } = 20;
}