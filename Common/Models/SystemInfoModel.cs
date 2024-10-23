namespace Common.Models;

public class SystemInfoModel: BaseMessage
{
    public required string OsName { get; set; }
    public string OSVersion { get; set; }
    public string Architecture { get; set; }
    public double Uptime { get; set; }
    public int ProcessCount { get; set; }
    public string Platform { get; set; }
}

