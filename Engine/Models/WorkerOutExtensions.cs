using Common.DTOs;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.DAL.Entities;

namespace Engine.Models;

public static class WorkerOutExtensions
{
    public static WorkerChangeEvent ToWorkerEvent(this WorkerEntity workerEntity, Guid engineId,
        WorkerState state, ChangeEventType changeEventType = ChangeEventType.Updated)
    {
        return new WorkerChangeEvent
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
            ChangeEventType = changeEventType,
            WatchdogEventCount = workerEntity.WatchdogEventCount,
            ImgWatchdogEnabled = workerEntity.ImgWatchdogEnabled,
            ImgWatchdogGraceTime = workerEntity.ImgWatchdogGraceTime,
            ImgWatchdogInterval = workerEntity.ImgWatchdogInterval
        };
    }


    public static WorkerChangeEvent ToWorkerEvent(this WorkerEntity workerEntity,
        WorkerState state, ChangeEventType changeEventType = ChangeEventType.Updated)
    {
        return new WorkerChangeEvent
        {
            WorkerId = workerEntity.WorkerId,
            Name = workerEntity.Name,
            Description = workerEntity.Description,
            Command = workerEntity.Command,
            IsEnabled = workerEntity.IsEnabled,
            State = state,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 0,
            ChangeEventType = changeEventType,
            WatchdogEventCount = workerEntity.WatchdogEventCount,
            ImgWatchdogEnabled = workerEntity.ImgWatchdogEnabled,
            ImgWatchdogGraceTime = workerEntity.ImgWatchdogGraceTime,
            ImgWatchdogInterval = workerEntity.ImgWatchdogInterval
        };
    }
}