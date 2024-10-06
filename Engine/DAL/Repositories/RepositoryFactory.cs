using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.DAL.Repositories;

public class RepositoryFactory
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
}
