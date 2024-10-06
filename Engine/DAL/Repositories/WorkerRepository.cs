using Engine.DAL.Entities;
using Engine.Database;

namespace Engine.DAL.Repositories;

using Microsoft.EntityFrameworkCore;


    public class WorkerRepository
    {
        private readonly ApplicationDbContext _context;

        public WorkerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WorkerEntity?> GetWorkerByIdAsync(string workerId)
        {
            return await _context.Workers.FirstOrDefaultAsync(w => w.WorkerId == workerId);
        }

        public async Task<List<WorkerEntity>> GetAllWorkersAsync()
        {
            return await _context.Workers.ToListAsync();
        }

        public async Task AddWorkerAsync(WorkerEntity worker)
        {
            await _context.Workers.AddAsync(worker);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateWorkerAsync(WorkerEntity worker)
        {
            worker.UpdatedAt = DateTime.UtcNow; // Opdater tidstempel ved enhver ændring
            _context.Workers.Update(worker);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteWorkerAsync(string workerId)
        {
            var worker = await GetWorkerByIdAsync(workerId);
            if (worker != null)
            {
                _context.Workers.Remove(worker);
                await _context.SaveChangesAsync();
            }
        }
    }
    