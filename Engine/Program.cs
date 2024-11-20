using Engine.Components;
using Engine.DependencyInjection;
using Engine.Hubs;
using Engine.Models;
using Engine.Services;
using Engine.Utils;

// Få basePath fra miljøvariablen eller brug fallback
var basePath = Environment.GetEnvironmentVariable("HIVE_BASE_PATH") ?? @"yC:\temp\hive";
var tempFolderPath = Path.Combine(basePath, "temp_test");
Console.WriteLine($"Base path: {basePath}");

try
{
    // Kontroller, om basePath eksisterer, eller opret den
    if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
    if (!Directory.Exists(tempFolderPath)) Directory.CreateDirectory(tempFolderPath);

    // Test skrive- og sletteadgang i den midlertidige mappe
    var testFilePath = Path.Combine(tempFolderPath, "write_test.tmp");
    File.WriteAllText(testFilePath, "Test");
    File.Delete(testFilePath);

    // Slet den midlertidige mappe efter testen
    Directory.Delete(tempFolderPath);
    Console.WriteLine("Skrive- og sletteadgang til basePath er OK.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Kan ikke få adgang til basePath eller skrive til mappen: {ex.Message}");
    Environment.Exit(1); // Afslut applikationen med en fejlstatus
}


var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilogLogging(basePath);

// Forsøg at finde GStreamer-eksekverbaren
GStreamerConfig.Instance.ExecutablePath = GStreamerUtils.FindGStreamerExecutable();
GStreamerConfig.Instance.SetTempPath(tempFolderPath);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDatabase(basePath);
builder.Services.AddApplicationServicesPreBuild();

var app = builder.Build();
app.InitializeDatabase();
ServiceCollectionExtensions.InitializeEngineId(app.Services);

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