using Engine.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engine.Database;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        var engineEntity = context.EngineEntities.Include(e => e.HubUrls).FirstOrDefault();
        if (engineEntity == null)
        {
            engineEntity = new EngineEntities
            {
                EngineId = Guid.NewGuid(),
                Name = "Default Engine",
                Version = "1.0",
                Description = "Default Description",
                InstallDate = DateTime.Now,
                HubUrls = new List<HubUrlEntity>
                {
                    new() { HubUrl = "http://127.0.0.1:9000/streamhub" }
                }
            };

            context.EngineEntities.Add(engineEntity);
            context.SaveChanges();
        }
        else if (!engineEntity.HubUrls.Any())
        {
            engineEntity.HubUrls.Add(new HubUrlEntity { HubUrl = "http://127.0.0.1:9000/streamhub" });
            context.SaveChanges();
        }
    }
}