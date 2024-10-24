using System.ComponentModel.DataAnnotations;

namespace Engine.DAL.Entities;

public class EngineEntities
{
    [Key]
    public Guid EngineId { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public required string Description { get; set; }
    public DateTime InstallDate { get; set; }
    public List<HubUrlEntity> HubUrls { get; set; } = new();  // New field to manage multiple URLs
}

public class HubUrlEntity
{
    [Key]
    public int Id { get; set; }
    [Required]
    public required string HubUrl { get; set; }
    public string ApiKey { get; set; } = "";
}