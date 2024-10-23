namespace Common.Models;

public class EngineBaseInfo: BaseMessage
{
    public string EngineName { get; set; } = "New Engine";
    public string EngineVersion { get; set; } = "1.0";
    public string EngineDescription { get; set; } = "Beskrivelse";
    public DateTime EngineStartDate { get; set; } = DateTime.Now;
    public string Version { get; set; }
    public DateTime InstallDate { get; set; }
    
    public List<HubUrlInfo> HubUrls { get; set; } = new();
}



public class HubUrlInfo
{
    public int Id { get; set; }
    public string HubUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}


public class EngineEvent : EngineBaseInfo
{
    public required EventType EventType { get; set; }
}