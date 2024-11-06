namespace Common.DTOs.Queries
{
    public class WorkerChangeLogEntry
    {
        public required DateTime ChangeTimestamp { get; init; }
        public required string ChangeDescription { get; init; }
        public required string ChangeDetails { get; init; }
    }

    public class WorkerChangeLog
    {
        public required string WorkerId { get; init; }
        public required List<WorkerChangeLogEntry> Changes { get; init; } = [];
    }
}