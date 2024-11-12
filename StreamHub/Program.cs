using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.ResponseCompression;
using StreamHub;
using StreamHub.Components;
using StreamHub.Hubs;
using StreamHub.Services;

var basePath = Environment.GetEnvironmentVariable("HIVE_BASE_PATH") ?? @"C:\temp\hive";
Console.WriteLine($"Base path: {basePath}");
if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
var streamHubPath = Path.Combine(basePath, "StreamHub");
if (!Directory.Exists(streamHubPath)) Directory.CreateDirectory(streamHubPath);

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilogLogging(streamHubPath);

builder.Services.AddSingleton<TrackingCircuitHandler>(); // For direkte injection
builder.Services.AddSingleton<CircuitHandler>(sp =>
    sp.GetRequiredService<TrackingCircuitHandler>()); // As CircuitHandler
builder.Services.AddScoped<BlazorSignalRService>();


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 256;
    options.MaximumParallelInvocationsPerClient = 10;
    options.EnableDetailedErrors = true;
}).AddMessagePackProtocol();
builder.Services.AddSingleton<IEngineManager, EngineManager>(); // Singleton for shared state
builder.Services.AddSingleton<CancellationService>(); // Singleton for shared cancellation token
builder.Services.AddSingleton<WorkerService>(); // Singleton for shared cancellation token

builder.Services.AddTransient<FrontendHandlers>();
builder.Services.AddTransient<BackendHandlers>();


builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<EngineHub>("/streamhub");
app.Run();