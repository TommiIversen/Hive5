using System.ComponentModel.DataAnnotations;


namespace Engine.DAL.Entities
{
    public class EngineEntities
    {
        [Key]
        public Guid EngineId { get; set; }
        public required string Name { get; set; } 
        public required string Version { get; set; }
        public required string Description { get; set; } 
        public DateTime InstallDate { get; set; } 
    }
}