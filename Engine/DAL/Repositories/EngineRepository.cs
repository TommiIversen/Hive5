using Engine.DAL.Entities;
using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.DAL.Repositories;

public interface IEngineRepository
{
    Task<EngineEntities?> GetEngineAsync();
    Task SaveEngineAsync(EngineEntities engine);
}


public class EngineRepository(ApplicationDbContext context) : IEngineRepository
{
    public async Task<EngineEntities?> GetEngineAsync()
    {
        return await context.EngineEntities
            .AsNoTracking() // Tilføj No Tracking for at undgå caching
            .Include(e => e.HubUrls)
            .FirstOrDefaultAsync();    }

    public async Task SaveEngineAsync(EngineEntities engine)
    {
        context.EngineEntities.Update(engine);
        await context.SaveChangesAsync();
    }
}
