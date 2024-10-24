using Engine.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engine.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<EngineEntities> EngineEntities { get; set; }
        public DbSet<WorkerEntity> Workers { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Opret et unikt indeks på HubUrl i HubUrlEntity
            modelBuilder.Entity<HubUrlEntity>()
                .HasIndex(h => h.HubUrl)
                .IsUnique(); // Sørg for, at HubUrl er unik
        }
    }
}