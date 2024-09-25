namespace Common.Models;

public class EngineBaseInfo
{
    public required Guid EngineId { get; set; }
    public string EngineName { get; set; } = "New Engine";
    public string EngineVersion { get; set; } = "1.0";
    public string EngineDescription { get; set; } = "Beskrivelse";
    public DateTime EngineStartDate { get; set; } = DateTime.Now;
}