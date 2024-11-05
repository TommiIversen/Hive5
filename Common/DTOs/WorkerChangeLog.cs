namespace Common.DTOs;

public class WorkerChangeLogDto
{
    public DateTime ChangeTimestamp { get; set; }
    public required string ChangeDescription { get; set; }
    public string? ChangeDetails { get; set; }
}

public class WorkerChangeLogsDto
{
    public required string WorkerId { get; set; }
    public List<WorkerChangeLogDto> Changes { get; set; } = new();
}