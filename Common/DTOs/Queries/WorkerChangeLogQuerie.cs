namespace Common.DTOs.Queries;

public class WorkerChangeLogDto
{
    public required DateTime ChangeTimestamp { get; init; }
    public required string ChangeDescription { get; init; }
    public required string ChangeDetails { get; init; }
}

public class WorkerChangeLogsDto
{
    public required string WorkerId { get; init; }
    public required List<WorkerChangeLogDto> Changes { get; init; } = [];
}