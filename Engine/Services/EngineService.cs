using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.Services;

public interface IEngineService
{
    Task<EngineEntities?> GetEngineAsync();
    Task UpdateEngineAsync(string name, string description);
    Task AddHubUrlAsync(string hubUrl, string apiKey);
    Task RemoveHubUrlAsync(int hubUrlId);
    Task EditHubUrlAsync(int hubUrlId, string newHubUrl, string newApiKey);
}
public class EngineService : IEngineService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IEngineRepository _engineRepository;

    public EngineService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _engineRepository = new EngineRepository(_contextFactory.CreateDbContext());
    }

    public async Task<EngineEntities?> GetEngineAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await _engineRepository.GetEngineAsync();
    }

    public async Task UpdateEngineAsync(string name, string description)
    {
        var engine = await _engineRepository.GetEngineAsync();
        if (engine != null)
        {
            engine.Name = name;
            engine.Description = description;
            await _engineRepository.SaveEngineAsync(engine);  // Kalder Save i stedet for en speciel Update-metode
        }
    }

    public async Task AddHubUrlAsync(string hubUrl, string apiKey)
    {
        var engine = await _engineRepository.GetEngineAsync();
        if (engine != null)
        {
            engine.HubUrls.Add(new HubUrlEntity { HubUrl = hubUrl, ApiKey = apiKey });
            await _engineRepository.SaveEngineAsync(engine);
        }
    }

    public async Task RemoveHubUrlAsync(int hubUrlId)
    {
        var engine = await _engineRepository.GetEngineAsync();
        var hubUrl = engine?.HubUrls.FirstOrDefault(h => h.Id == hubUrlId);
        if (hubUrl != null)
        {
            engine.HubUrls.Remove(hubUrl);
            await _engineRepository.SaveEngineAsync(engine);
        }
    }

    public async Task EditHubUrlAsync(int hubUrlId, string newHubUrl, string newApiKey)
    {
        var engine = await _engineRepository.GetEngineAsync();
        var hubUrl = engine?.HubUrls.FirstOrDefault(h => h.Id == hubUrlId);
        if (hubUrl != null)
        {
            hubUrl.HubUrl = newHubUrl;
            hubUrl.ApiKey = newApiKey;
            await _engineRepository.SaveEngineAsync(engine);
        }
    }
}
