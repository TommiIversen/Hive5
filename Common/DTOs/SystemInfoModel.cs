namespace Common.DTOs;

public class SystemInfoModel : BaseMessage
{
    public required string OsName { get; init; }
    public required string OsVersion { get; init; }
    public required string Architecture { get; init; }
    public required double Uptime { get; init; }
    public required int ProcessCount { get; init; }
    public required string Platform { get; init; }
}