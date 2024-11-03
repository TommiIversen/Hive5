using Engine.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engine.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<EngineEntities> EngineEntities { get; set; }
    public DbSet<WorkerEntity> Workers { get; set; }
    public DbSet<WorkerEvent> WorkerEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkerEntity>()
            .Property(w => w.ImgWatchdogEnabled)
            .HasDefaultValue(true);

        modelBuilder.Entity<WorkerEntity>()
            .Property(w => w.ImgWatchdogGraceTime)
            .HasDefaultValue(TimeSpan.FromSeconds(10));

        modelBuilder.Entity<WorkerEntity>()
            .Property(w => w.ImgWatchdogInterval)
            .HasDefaultValue(TimeSpan.FromSeconds(5));
        
        // Opret et unikt indeks på HubUrl i HubUrlEntity
        modelBuilder.Entity<HubUrlEntity>()
            .HasIndex(h => h.HubUrl)
            .IsUnique(); // Sørg for, at HubUrl er unik
    }
}