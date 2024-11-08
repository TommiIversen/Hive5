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
    public DbSet<WorkerChangeLog> WorkerChangeLogs { get; set; } // Ny ændringslog-tabel


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


        modelBuilder.Entity<WorkerEntity>()
            .HasMany(w => w.Events)
            .WithOne()
            .HasForeignKey(e => e.WorkerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkerEntity>()
            .HasMany(w => w.Events)
            .WithOne()
            .HasForeignKey(e => e.WorkerId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete på WorkerEvents

        modelBuilder.Entity<WorkerEvent>()
            .HasMany(e => e.EventLogs)
            .WithOne()
            .HasForeignKey(log => log.EventId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete på EventLogs


        modelBuilder.Entity<WorkerEntity>()
            .HasMany(w => w.ChangeLogs)
            .WithOne()
            .HasForeignKey(c => c.WorkerId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete for ændringslogs

        modelBuilder.Entity<WorkerChangeLog>()
            .Property(c => c.ChangeDescription)
            .HasMaxLength(255); // Optional begrænsning på længden af ChangeDescription
    }
}