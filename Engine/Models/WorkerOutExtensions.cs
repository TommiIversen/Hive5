using Engine.DAL.Entities;
using Engine.Utils;

namespace Common.Models;

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
    
    
    public static WorkerEvent ToWorkerEvent(this WorkerEntity workerEntity, Guid engineId, 
        WorkerEventType eventType = WorkerEventType.Updated)
    {
        return new WorkerEvent
        {
            WorkerId = workerEntity.WorkerId,
            Name = workerEntity.Name,
            Description = workerEntity.Description,
            Command = workerEntity.Command,
            Enabled = workerEntity.IsEnabled,
            State = StreamerState.Idle, // Som standard Idle; kan opdateres, hvis nødvendigt
            EngineId = engineId,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 0, // Dette kan justeres for en specifik rækkefølge
            EventType = eventType
        };
    }
}