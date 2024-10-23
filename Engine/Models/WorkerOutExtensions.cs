using Common.Models;
using Engine.DAL.Entities;
using Engine.Utils;

namespace Engine.Models;

public static class WorkerOutExtensions
{
    
    
    public static WorkerEvent ToWorkerEvent(this WorkerEntity workerEntity, Guid engineId, 
        WorkerState state, EventType eventType = EventType.Updated)
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
            EventType = eventType,
            WatchdogEventCount = workerEntity.WatchdogEventCount
        };
    }
    
    
    public static WorkerEvent ToWorkerEvent(this WorkerEntity workerEntity, 
        WorkerState state, EventType eventType = EventType.Updated)
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
            EventType = eventType,
            WatchdogEventCount = workerEntity.WatchdogEventCount
        };
    }
}