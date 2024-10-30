using System.ComponentModel.DataAnnotations;

namespace Engine.DAL.Entities;

public class WorkerEntity
{
    [Key]
    public required string WorkerId { get; set; } = Guid.NewGuid().ToString();
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Automatisk tidspunkt for oprettelse
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Automatisk opdateringstidspunkt
    public int WatchdogEventCount { get; set; } = 0;
    
    // Watchdog-indstillinger
    public bool ImgWatchdogEnabled { get; set; }
    public TimeSpan ImgWatchdogGraceTime { get; set; }
    public TimeSpan ImgWatchdogInterval { get; set; }
}