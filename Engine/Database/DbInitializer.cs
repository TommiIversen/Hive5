using Engine.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engine.Database;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // Hvis der allerede er en EngineEntity, gøres der intet
        var engineEntity = context.EngineEntities.Include(e => e.HubUrls).FirstOrDefault();
        if (engineEntity == null)
        {
            // Seed standarddata
            engineEntity = new EngineEntities
            {
                EngineId = Guid.NewGuid(),
                Name = "Default Engine",
                Version = "1.0",
                Description = "Default Description",
                InstallDate = DateTime.Now,
                HubUrls = [new HubUrlEntity {HubUrl = "http://127.0.0.1:9000/streamhub"}]
            };

            context.EngineEntities.Add(engineEntity);
            context.SaveChanges();
        }
        else if (!engineEntity.HubUrls.Any())
        {
            // Hvis der ikke er nogen HubUrls, tilføjes en default
            engineEntity.HubUrls.Add(new HubUrlEntity {HubUrl = "http://127.0.0.1:9000/streamhub"});
            context.SaveChanges();
        }
    }
}