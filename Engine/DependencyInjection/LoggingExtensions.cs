using Serilog;
using Serilog.Events;

namespace Engine.DependencyInjection;

public static class LoggingExtensions
{
    public static void ConfigureSerilogLogging(this WebApplicationBuilder builder, string basePath)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(basePath, "logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        builder.Host.UseSerilog(Log.Logger);
        Log.Information("Blazor server applikation starter op...");
    }
}