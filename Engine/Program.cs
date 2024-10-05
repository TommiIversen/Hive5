using Common.Models;
using Engine.Components;
using Engine.Database;
using Engine.Hubs;
using Engine.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Definér base path, hvor data skal gemmes
var basePath = @"C:\temp\hive";

// Opret mappen, hvis den ikke eksisterer
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


// Hent maskinens navn og brug det til databasefilen, og gem den i basePath
var machineName = Environment.MachineName;
var dbFileName = Path.Combine(basePath, $"{machineName}.db"); // Brug basePath til databasefilen

// Registrer DbContext med maskinens navn som databasefilnavn
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbFileName}")); // SQLite-database med base path


// Registrer MessageQueue som singleton og angiv max størrelse
int maxQueueSize = 10;
builder.Services.AddSingleton<MessageQueue>(provider => new MessageQueue(maxQueueSize));

builder.Services.AddSingleton<WorkerManager>();


// URLs for StreamHub connections
var streamHubUrls = new List<string>
{
    "http://127.0.0.1:9000/streamhub",
    "http://127.0.0.1:8999/streamhub"
};

// Registrer StreamHub med injektion af loggerFactory og MessageQueue
builder.Services.AddSingleton<StreamHub>(provider => new StreamHub(
    provider.GetRequiredService<MessageQueue>(),
    provider.GetRequiredService<ILogger<StreamHub>>(),
    provider.GetRequiredService<ILoggerFactory>(),
    streamHubUrls,
    20,
    provider.GetRequiredService<WorkerManager>() // Injicér WorkerManager via provider
));


builder.Services.AddSingleton<MetricsService>();

var app = builder.Build();


// Opret database og seed data, hvis nødvendigt
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Anvend migrationer og opret databasen, hvis den ikke findes
    dbContext.Database.Migrate();

    // Seed data ved første init
    DbInitializer.Seed(dbContext);
}

// Retrieve the StreamHub instance and initialize it
app.Services.GetRequiredService<StreamHub>();

var metricsService = app.Services.GetRequiredService<MetricsService>();
metricsService.Start();

var workerManager = app.Services.GetRequiredService<WorkerManager>();

Log.Information("Creating workers...");

var workerCreate1 = new WorkerCreate(name: "Worker1", description: "Desc", command: "gstreamer");
var workerCreate2 = new WorkerCreate(name: "Worker2", description: "Desc2", command: "gstreamer2");
var workerCreate3 = new WorkerCreate(name: "Worker3", description: "Desc3", command: "gstreamer3");

var worker1 = workerManager.AddWorker(workerCreate1);
var worker2 = workerManager.AddWorker(workerCreate2);
var worker3 = workerManager.AddWorker(workerCreate3);
workerManager.StartWorker(worker1.WorkerId);
workerManager.StartWorker(worker2.WorkerId);
workerManager.StartWorker(worker3.WorkerId);

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


