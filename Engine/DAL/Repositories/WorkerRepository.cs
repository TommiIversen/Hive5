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
    Task<List<WorkerEventLogDto>> GetRecentWorkerEventsWithLogsAsync(string workerId, int maxEvents = 20);
    Task<List<WorkerChangeLog>> GetWorkerChangeLogAsync(string workerId, int maxEntries = 50);
    Task AddWorkerChangeLogsAsync(IEnumerable<WorkerChangeLog> changeLogs);
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
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var worker = await context.Workers
                .Include(w => w.Events)
                .FirstOrDefaultAsync(w => w.WorkerId == workerId);

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
                var excessEvents = worker.Events
                    .OrderBy(e => e.EventTimestamp)
                    .Take(worker.Events.Count - 20)
                    .ToList();
                context.WorkerEvents.RemoveRange(excessEvents);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync(); // Commit transaktionen
        }
        catch
        {
            await transaction.RollbackAsync(); // Rul tilbage ved fejl
            throw;
        }
    }


    public async Task<List<WorkerEventLogDto>> GetRecentWorkerEventsWithLogsAsync(string workerId, int maxEvents = 20)
    {
        return await context.WorkerEvents.AsNoTracking()
            .Where(e => e.WorkerId == workerId)
            .OrderByDescending(e => e.EventTimestamp)
            .Take(maxEvents)
            .Select(e => new WorkerEventLogDto
            {
                EventTimestamp = e.EventTimestamp,
                EventMessage = e.Message,
                Logs = e.EventLogs.Select(log => new EventLogEntry
                {
                    Message = log.Message,
                    LogLevel = (int) log.LogLevel,
                    LogTimestamp = log.LogTimestamp
                }).ToList()
            })
            .ToListAsync();
    }
    
    public async Task<List<WorkerChangeLog>> GetWorkerChangeLogAsync(string workerId, int maxEntries = 50)
    {
        return await context.WorkerChangeLogs
            .Where(log => log.WorkerId == workerId)
            .OrderByDescending(log => log.ChangeTimestamp)
            .Take(maxEntries)
            .ToListAsync();
    }
    
    public async Task AddWorkerChangeLogsAsync(IEnumerable<WorkerChangeLog> changeLogs)
    {
        await context.WorkerChangeLogs.AddRangeAsync(changeLogs);
        await context.SaveChangesAsync();
    }
}