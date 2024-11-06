namespace Common.DTOs;

public class EngineBaseInfo : BaseMessage
{
    public required string EngineName { get; init; }
    public required string EngineDescription { get; init; }
    public DateTime EngineStartDate { get; init; } = DateTime.Now;
    public required string Version { get; init; }
    public required DateTime InstallDate { get; init; }
    public List<HubUrlInfo> HubUrls { get; set; } = [];
}

public class HubUrlInfo
{
    public required int Id { get; init; }
    public required string HubUrl { get; init; } = string.Empty;
    public required string ApiKey { get; init; } = string.Empty;
}

public class EngineEvent : EngineBaseInfo
{
    public required EventType EventType { get; init; }
}