using Engine.DAL.Entities;

namespace Engine.Database;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // Hvis der allerede er en EngineEntity, gøres der intet
        if (context.EngineEntities.Any())
        {
            return;   // DB er allerede seed'et
        }

        // Seed standarddata
        var engineEntity = new EngineEntities
        {
            EngineId = Guid.NewGuid(),
            Name = "Default Engine",  // Standard navn
            Version = "1.0",
            Description = "Default Description",
            InstallDate = DateTime.Now
        };

        context.EngineEntities.Add(engineEntity);
        context.SaveChanges();
    }
}