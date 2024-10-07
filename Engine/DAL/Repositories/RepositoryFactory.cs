using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.DAL.Repositories;

public class RepositoryFactory : IRepositoryFactory
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public RepositoryFactory(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IWorkerRepository CreateWorkerRepository()
    {
        var dbContext = _contextFactory.CreateDbContext();
        return new WorkerRepository(dbContext);
    }

    public IEngineRepository CreateEngineRepository()
    {
        var dbContext = _contextFactory.CreateDbContext();
        return new EngineRepository(dbContext);
    }
}

public interface IRepositoryFactory
{
}