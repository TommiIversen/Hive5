using Engine.DAL.Entities;
using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.DAL.Repositories;

public interface IEngineRepository
{
    Task<EngineEntities?> GetEngineAsync();
    Task UpdateEngineAsync(string name, string description);
}

public class EngineRepository(ApplicationDbContext context) : IEngineRepository
{
    public async Task<EngineEntities?> GetEngineAsync()
    {
        // Henter den eneste EngineEntity i databasen
        return await context.EngineEntities.FirstOrDefaultAsync();
    }

    public async Task UpdateEngineAsync(string name, string description)
    {
        var engine = await GetEngineAsync();
        if (engine != null)
        {
            engine.Name = name;
            engine.Description = description;
            await context.SaveChangesAsync();
        }
    }
}