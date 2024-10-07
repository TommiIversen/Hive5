using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.DAL.Repositories;

public class RepositoryFactory(IDbContextFactory<ApplicationDbContext> contextFactory)
{
    public IWorkerRepository CreateWorkerRepository()
    {
        var dbContext = contextFactory.CreateDbContext();
        return new WorkerRepository(dbContext);
    }
}
