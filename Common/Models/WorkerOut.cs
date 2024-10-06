using Engine.Utils;

namespace Common.Models;

public class WorkerOut : BaseMessage
{
    public required string WorkerId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Command { get; set; }
    public bool Enabled { get; set; }
    public StreamerState State { get; set; }
}