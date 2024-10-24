namespace Common.DTOs;

public class SystemInfoModel: BaseMessage
{
    public required string OsName { get; set; }
    public required string OSVersion { get; set; }
    public required string Architecture { get; set; }
    public double Uptime { get; set; }
    public int ProcessCount { get; set; }
    public required string Platform { get; set; }
}
