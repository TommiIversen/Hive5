using System.ComponentModel.DataAnnotations;

namespace Engine.DAL.Entities;

public class WorkerChangeLog
{
    [Key] public int ChangeLogId { get; set; }
    public required string WorkerId { get; set; }
    public DateTime ChangeTimestamp { get; set; } = DateTime.UtcNow;
    public required string ChangeDescription { get; set; }
    public string ChangeDetails { get; set; } = string.Empty;
}
