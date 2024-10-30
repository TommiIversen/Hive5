using Engine.Components;
using Engine.Database;
using Engine.DependencyInjection;
using Engine.Hubs;
using Engine.Services;
using Serilog;

// Få basePath fra miljøvariablen eller brug fallback
var basePath = Environment.GetEnvironmentVariable("HIVE_BASE_PATH") ?? @"C:\temp\hive";
Console.WriteLine($"Base path: {basePath}");
if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

// Initialiser SQLite, hvis nødvendigt
// Batteries.Init();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilogLogging(basePath);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDatabase(basePath);
builder.Services.AddApplicationServicesPreBuild();

var app = builder.Build();
app.InitializeDatabase();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var engineEntity = dbContext.EngineEntities.FirstOrDefault();

    if (engineEntity != null)
    {
        var loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService>();
        loggerService.SetEngineId(engineEntity.EngineId);
    }
    else
    {
        Log.Warning("Ingen EngineId fundet under initialisering.");
    }
}
// Registrer de services, der kræver database-adgang, efter `InitializeDatabase` og før appen kører.

app.Services.GetRequiredService<StreamHub>();

var workerManager = app.Services.GetRequiredService<IWorkerManager>();
await workerManager.InitializeWorkersAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();