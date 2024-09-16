using Engine.Components;
using Engine.Models;
using Engine.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<StreamHubService>();


var app = builder.Build();

// Retrieve the StreamHubService instance and initialize it
var streamHubService = app.Services.GetRequiredService<StreamHubService>();

// Start background task to connect to StreamHub without blocking
_ = Task.Run(async () => await streamHubService.StartAsync());

// Create two workers and start them
var worker1 = new Worker(streamHubService);
var worker2 = new Worker(streamHubService);
streamHubService.AddWorker(worker1);
streamHubService.AddWorker(worker2);
worker1.Start();
worker2.Start();


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
