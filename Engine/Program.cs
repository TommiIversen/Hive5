using Engine.Components;
using Engine.DependencyInjection;
using Engine.Hubs;
using Engine.Services;

// Få basePath fra miljøvariablen eller brug fallback
var basePath = Environment.GetEnvironmentVariable("HIVE_BASE_PATH") ?? @"C:\temp\hive";
Console.WriteLine($"Base path: {basePath}");
if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

// Initialiser SQLite, hvis nødvendigt
// Batteries.Init();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilogLogging(basePath);

// Initialiser SQLite
//Batteries.Init();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDatabase(basePath);
builder.Services.AddApplicationServices();

var app = builder.Build();

app.InitializeDatabase();

app.Services.GetRequiredService<StreamHub>();

var metricsService = app.Services.GetRequiredService<MetricsService>();
metricsService.Start();

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