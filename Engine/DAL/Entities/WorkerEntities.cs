using System.ComponentModel.DataAnnotations;

namespace Engine.DAL.Entities;

public class WorkerEntity
{
    [Key]
    public required string WorkerId { get; set; } = Guid.NewGuid().ToString(); // Hvis ingen WorkerId er angivet ved oprettelse, genereres en ny GUID
    public required string Name { get; set; }
    public string Description { get; set; }
    public string Command { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Automatisk tidspunkt for oprettelse
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Automatisk opdateringstidspunkt
}