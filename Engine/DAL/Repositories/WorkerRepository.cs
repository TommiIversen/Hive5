using Common.DTOs;
using Engine.DAL.Entities;
using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.DAL.Repositories;

public interface IWorkerRepository
{
    Task<WorkerEntity?> GetWorkerByIdAsync(string workerId);
    Task<List<WorkerEntity>> GetAllWorkersAsync();
    Task AddWorkerAsync(WorkerEntity worker);
    Task UpdateWorkerAsync(WorkerEntity worker);
    Task DeleteWorkerAsync(string workerId);
    Task AddWorkerEventAsync(string workerId, string message, List<BaseLogEntry> logs);
    Task<WorkerEntity?> GetWorkerByIdWithEventsAsync(string workerId);
}

public class WorkerRepository(ApplicationDbContext context) : IWorkerRepository
{
    public async Task<WorkerEntity?> GetWorkerByIdAsync(string workerId)
    {
        Console.WriteLine($"Data in cache:  {context.Workers.Local.Count}");
        return await context.Workers.AsNoTracking().FirstOrDefaultAsync(w => w.WorkerId == workerId);
    }


    public async Task<List<WorkerEntity>> GetAllWorkersAsync()
    {
        return await context.Workers.AsNoTracking().ToListAsync();
    }

    public async Task AddWorkerAsync(WorkerEntity worker)
    {
        await context.Workers.AddAsync(worker);
        await context.SaveChangesAsync();
    }


    public async Task UpdateWorkerAsync(WorkerEntity worker)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            worker.UpdatedAt = DateTime.UtcNow;
            context.Workers.Update(worker);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
    
    public async Task AddWorkerEventAsync(string workerId, string message, List<BaseLogEntry> logs)
    {
        var worker = await context.Workers.Include(w => w.Events).FirstOrDefaultAsync(w => w.WorkerId == workerId);
        if (worker == null) throw new InvalidOperationException("Worker not found");

        var newEvent = new WorkerEvent
        {
            WorkerId = workerId,
            Message = message,
            EventLogs = logs.Select(log => new WorkerEventLog
            {
                LogTimestamp = log.LogTimestamp,
                LogLevel = log.LogLevel,
                Message = log.Message
            }).ToList()
        };

        worker.Events.Add(newEvent);

        // Begræns til de sidste 20 hændelser
        if (worker.Events.Count > 20)
        {
            var excessEvents = worker.Events.OrderBy(e => e.EventTimestamp).Take(worker.Events.Count - 20).ToList();
            context.WorkerEvents.RemoveRange(excessEvents);
        }

        await context.SaveChangesAsync();
    }
    
    public async Task<WorkerEntity?> GetWorkerByIdWithEventsAsync(string workerId)
    {
        return await context.Workers
            .AsNoTracking()
            .Include(w => w.Events)
            .ThenInclude(e => e.EventLogs)
            .FirstOrDefaultAsync(w => w.WorkerId == workerId);
    }
    
}