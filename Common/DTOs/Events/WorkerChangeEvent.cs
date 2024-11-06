using Common.DTOs.Enums;

namespace Common.DTOs.Events;

public class WorkerChangeEvent : WorkerInfo
{
    public required ChangeEventType ChangeEventType { get; init; }
}