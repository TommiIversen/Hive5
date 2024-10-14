using Common.Models;
using Engine.DAL.Entities;
using Engine.Utils;

namespace Engine.Models;

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
            IsEnabled = workerOut.IsEnabled,
            State = workerOut.State,
            EngineId = workerOut.EngineId,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = workerOut.SequenceNumber,
            EventType = eventType
        };
    }
    
    
    public static WorkerEvent ToWorkerEvent(this WorkerEntity workerEntity, Guid engineId, 
        WorkerState state, WorkerEventType eventType = WorkerEventType.Updated)
    {
        return new WorkerEvent
        {
            WorkerId = workerEntity.WorkerId,
            Name = workerEntity.Name,
            Description = workerEntity.Description,
            Command = workerEntity.Command,
            IsEnabled = workerEntity.IsEnabled,
            State = state,
            EngineId = engineId,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 0,
            EventType = eventType
        };
    }
    
    
    public static WorkerEvent ToWorkerEvent(this WorkerEntity workerEntity, 
        WorkerState state, WorkerEventType eventType = WorkerEventType.Updated)
    {
        return new WorkerEvent
        {
            WorkerId = workerEntity.WorkerId,
            Name = workerEntity.Name,
            Description = workerEntity.Description,
            Command = workerEntity.Command,
            IsEnabled = workerEntity.IsEnabled,
            State = state,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 0,
            EventType = eventType
        };
    }
}