using System.ComponentModel.DataAnnotations;

namespace Engine.DAL.Entities;

public class WorkerEntity
{
    [Key]
    [Required]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "WorkerId skal være mellem 4 og 50 tegn.")]
    public required string WorkerId { get; init; }

    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int WatchdogEventCount { get; set; } = 0;

    // Watchdog-indstillinger
    public bool ImgWatchdogEnabled { get; set; }
    public required TimeSpan ImgWatchdogGraceTime { get; set; }
    public required TimeSpan ImgWatchdogInterval { get; set; }

    [Required]
    [StringLength(100)] // Set max length for Name
    public string StreamerType { get; set; } = "FakeStreamer";

    public List<WorkerEvent> Events { get; set; } = new();
    public List<WorkerChangeLog> ChangeLogs { get; set; } = new();
}