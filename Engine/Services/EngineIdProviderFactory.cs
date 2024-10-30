using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.Services;

public interface IEngineIdProviderFactory
{
    IEngineIdProvider CreateEngineIdProvider();
}

public class EngineIdProviderFactory : IEngineIdProviderFactory
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public EngineIdProviderFactory(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public IEngineIdProvider CreateEngineIdProvider()
    {
        Console.WriteLine("CreateEngineIdProvider: Henter EngineId fra databasen...");
        using var dbContext = _dbContextFactory.CreateDbContext();
            
        var engineEntity = dbContext.EngineEntities.FirstOrDefault();
        if (engineEntity == null)
        {
            throw new InvalidOperationException("EngineId kunne ikke hentes fra databasen.");
        }

        return new EngineIdProvider(engineEntity.EngineId);
    }
}