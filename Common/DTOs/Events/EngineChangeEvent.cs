using Common.DTOs.Enums;

namespace Common.DTOs.Events;

public class EngineChangeEvent : BaseEngineInfo
{
    public required ChangeEventType ChangeEventType { get; init; }
}