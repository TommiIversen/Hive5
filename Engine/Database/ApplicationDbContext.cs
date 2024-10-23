using Engine.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engine.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<EngineEntities> EngineEntities { get; set; }
        public DbSet<WorkerEntity> Workers { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
    }
}