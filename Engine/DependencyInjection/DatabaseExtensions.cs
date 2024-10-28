using Engine.Database;
using Engine.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Engine.DependencyInjection
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, string basePath)
        {
            var dbFileName = DbConstants.GetDatabaseFilePath(basePath);
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={dbFileName}"));

            return services;
        }

        public static void InitializeDatabase(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ryd eksisterende migrationslåse
            try
            {
                dbContext.Database.ExecuteSqlRaw("DELETE FROM __EFMigrationsLock");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Kunne ikke rydde migrationslåsen.");
            }

            // Anvend migrationer og seed data
            try
            {
                dbContext.Database.Migrate();
                DbInitializer.Seed(dbContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Migration failed.");
                throw;
            }
        }
    }
}