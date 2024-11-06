namespace Common.DTOs.Events;

public class EngineSystemInfoModel : BaseMessage
{
    public required string OsName { get; init; }
    public required string OsVersion { get; init; }
    public required string Architecture { get; init; }
    public required double Uptime { get; init; }
    public required int ProcessCount { get; init; }
    public required string Platform { get; init; }
}