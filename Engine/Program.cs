using Common.Models;
using Engine.Components;
using Engine.DAL.Repositories;
using Engine.Database;
using Engine.Hubs;
using Engine.Models;
using Engine.Services;
using Engine.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

var basePath = @"C:\temp\hive";
if (!Directory.Exists(basePath))
{
    Directory.CreateDirectory(basePath);
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information) // Microsoft-specifik logning
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning) // ASP.NET Core-specifik logning
    .WriteTo.Console() // Logger til konsol
    .WriteTo.File(
        path: Path.Combine(basePath, "logs", "log-.txt"), // Brug basePath til logs
        rollingInterval: RollingInterval.Day, // En fil per dag
        retainedFileCountLimit: 30) // Behold kun de sidste 30 dage
    .CreateLogger();

Log.Information("Blazor server applikation starter op...");
var builder = WebApplication.CreateBuilder(args);

// Initialiser SQLite, så det kan bruges korrekt
SQLitePCL.Batteries.Init();

// Tilføj Serilog som logger til DI
builder.Host.UseSerilog(Log.Logger);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<RepositoryFactory>();


// Hent maskinens navn og brug det til databasefilen, og gem den i basePath
var machineName = Environment.MachineName;
var dbFileName = Path.Combine(basePath, $"{machineName}.db"); // Brug basePath til databasefilen

// Registrer DbContext med maskinens navn som databasefilnavn
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbFileName}"));

// Registrer MessageQueue som singleton og angiv max størrelse

int maxQueueSize = 10;
builder.Services.AddSingleton<MessageQueue>(provider => new MessageQueue(maxQueueSize));
builder.Services.AddSingleton<WorkerManager>();
builder.Services.AddScoped<IEngineRepository, EngineRepository>();
builder.Services.AddSingleton<IEngineService, EngineService>();
builder.Services.AddSingleton<StreamHub>();

builder.Services.Configure<StreamHubOptions>(options =>
{
    options.HubUrls = new List<string>
    {
        "http://127.0.0.1:9000/streamhub",
        "http://127.0.0.1:8999/streamhub"
    };
    options.MaxQueueSize = 20;
});

// Registrer den konkrete implementering af INetworkInterfaceProvider til brug i MetricsService
builder.Services.AddSingleton<INetworkInterfaceProvider, NetworkInterfaceProvider>(); 
builder.Services.AddSingleton<MetricsService>();

var app = builder.Build();


// Opret database og seed data, hvis nødvendigt
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ryd eventuelt eksisterende låse før migrering
    try
    {
        dbContext.Database.ExecuteSqlRaw("DELETE FROM __EFMigrationsLock");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Kunne ikke rydde migrationslåsen.");
    }
    
    try
    {
        // Anvend migrationer og opret databasen, hvis den ikke findes
        dbContext.Database.Migrate();
        DbInitializer.Seed(dbContext);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Migration failed.");
        throw;
    }
}

// Retrieve the StreamHub instance and initialize it
app.Services.GetRequiredService<StreamHub>();

var metricsService = app.Services.GetRequiredService<MetricsService>();
metricsService.Start();

var workerManager = app.Services.GetRequiredService<WorkerManager>();

Log.Information("Creating workers...");
var workerCreate1 = new WorkerCreate(workerId: "w1",  name: "Worker1", description: "Desc", command: "gstreamer");
var workerCreate2 = new WorkerCreate(workerId: "w2", name: "Worker2", description: "Desc2", command: "gstreamer2");
var workerCreate3 = new WorkerCreate(workerId: "w3",name: "Worker3", description: "Desc3", command: "gstreamer3");
var worker1 = workerManager.AddWorker(workerCreate1);
var worker2 = workerManager.AddWorker(workerCreate2);
var worker3 = workerManager.AddWorker(workerCreate3);
await workerManager.StartWorkerAsync(worker1.WorkerId);
await workerManager.StartWorkerAsync(worker2.WorkerId);
await workerManager.StartWorkerAsync(worker3.WorkerId);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();