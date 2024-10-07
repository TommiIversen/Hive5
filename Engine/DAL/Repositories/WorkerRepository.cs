using Engine.DAL.Entities;
using Engine.Database;

namespace Engine.DAL.Repositories;

using Microsoft.EntityFrameworkCore;

public interface IWorkerRepository
{
    Task<WorkerEntity?> GetWorkerByIdAsync(string workerId);
    Task<List<WorkerEntity>> GetAllWorkersAsync();
    Task AddWorkerAsync(WorkerEntity worker);
    Task UpdateWorkerAsync(WorkerEntity worker);
    Task DeleteWorkerAsync(string workerId);
}

public class WorkerRepository(ApplicationDbContext context) : IWorkerRepository
{
    public async Task<WorkerEntity?> GetWorkerByIdAsync(string workerId)
    {
        Console.WriteLine($"Data in cache:  {context.Workers.Local.Count}");
        return await Task.FromResult(await context.Workers.FindAsync(workerId));
    }


    public async Task<List<WorkerEntity>> GetAllWorkersAsync()
    {
        return await context.Workers.ToListAsync();
    }

    public async Task AddWorkerAsync(WorkerEntity worker)
    {
        await context.Workers.AddAsync(worker);
        await context.SaveChangesAsync();
    }

    public async Task UpdateWorkerAsync(WorkerEntity worker)
    {
        worker.UpdatedAt = DateTime.UtcNow; // Opdater tidstempel ved enhver ændring
        context.Workers.Update(worker);
        await context.SaveChangesAsync();
    }

    public async Task DeleteWorkerAsync(string workerId)
    {
        var worker = await GetWorkerByIdAsync(workerId);
        if (worker != null)
        {
            context.Workers.Remove(worker);
            await context.SaveChangesAsync();
        }
    }
}