using Engine.Utils;

namespace Common.Models;

public enum WorkerEventType
{
    Created,
    Updated,
    Deleted
}

public class WorkerOut : BaseMessage
{
    public required string WorkerId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Command { get; set; }
    public bool Enabled { get; set; }
    public StreamerState State { get; set; }
}

public static class WorkerOutExtensions
{
    public static WorkerEvent ToWorkerEvent(this WorkerOut workerOut,
        WorkerEventType eventType = WorkerEventType.Updated)
    {
        return new WorkerEvent
        {
            WorkerId = workerOut.WorkerId,
            Name = workerOut.Name,
            Description = workerOut.Description,
            Command = workerOut.Command,
            Enabled = workerOut.Enabled,
            State = workerOut.State,
            EngineId = workerOut.EngineId,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = workerOut.SequenceNumber,
            EventType = eventType
        };
    }
}

public class WorkerEvent : WorkerOut
{
    public required WorkerEventType EventType { get; set; }
}