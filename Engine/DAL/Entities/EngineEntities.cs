using System.ComponentModel.DataAnnotations;


namespace Engine.DAL.Entities
{
    public class EngineEntities
    {
        [Key]
        public Guid EngineId { get; set; }
        public string Name { get; set; } 
        public string Version { get; set; }
        public string Description { get; set; } 
        public DateTime InstallDate { get; set; } 
    }
}