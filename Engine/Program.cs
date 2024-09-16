using Engine.Components;
using Engine.Services;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console() // Logger til konsol
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day, // En fil per dag
        retainedFileCountLimit: 30) // Behold kun de sidste 30 dage
    .CreateLogger();

Log.Information("Blazor server applikation starter op...");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<MessageQueue>();
builder.Services.AddSingleton<WorkerManager>();

builder.Services.AddSingleton<StreamHubService>();
builder.Services.AddSingleton<MetricsService>();

var app = builder.Build();

var messageQueue = app.Services.GetRequiredService<MessageQueue>();

// Retrieve the StreamHubService instance and initialize it
var streamHubService = app.Services.GetRequiredService<StreamHubService>();
_ = Task.Run(async () => await streamHubService.StartAsync());


var metricsService = app.Services.GetRequiredService<MetricsService>();
metricsService.Start();

var workerManager = app.Services.GetRequiredService<WorkerManager>();

Log.Information("Creating workers...");
var worker1 = workerManager.AddWorker();
var worker2 = workerManager.AddWorker();
workerManager.StartWorker(worker1.WorkerId);
workerManager.StartWorker(worker2.WorkerId);

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