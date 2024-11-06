using Common.DTOs;
using Common.DTOs.Enums;
using Common.DTOs.Events;
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
    Task<EngineEvent> GetEngineBaseInfoAsEvent();
}

public class EngineService : IEngineService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IEngineRepository _engineRepository;
    private readonly DateTime _initDateTime = DateTime.UtcNow;

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
            await _engineRepository.SaveEngineAsync(engine);
        }
    }

    public async Task AddHubUrlAsync(string hubUrl, string apiKey)
    {
        var engine = await _engineRepository.GetEngineAsync();
        if (engine != null)
        {
            engine.HubUrls.Add(new HubUrlEntity {HubUrl = hubUrl, ApiKey = apiKey});
            await _engineRepository.SaveEngineAsync(engine);
        }
    }

    public async Task RemoveHubUrlAsync(int hubUrlId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var hubUrl = await dbContext.Set<HubUrlEntity>().FindAsync(hubUrlId);

        if (hubUrl != null)
        {
            dbContext.Set<HubUrlEntity>().Remove(hubUrl);
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"Successfully removed HubUrlEntity with Id {hubUrlId}");
        }
        else
        {
            Console.WriteLine($"HubUrlEntity with Id {hubUrlId} not found");
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
            if (engine != null) await _engineRepository.SaveEngineAsync(engine);
        }
    }

    public async Task<EngineEvent> GetEngineBaseInfoAsEvent()
    {
        var engine = await GetEngineAsync();
        if (engine == null) throw new InvalidOperationException("Engine not found");

        var engineBaseInfo = new EngineEvent
        {
            EngineId = engine.EngineId,
            EngineName = engine.Name,
            EngineDescription = engine.Description,
            Version = engine.Version,
            InstallDate = engine.InstallDate,
            EngineStartDate = _initDateTime,
            HubUrls = engine.HubUrls.Select(h => new HubUrlInfo
                {
                    Id = h.Id,
                    HubUrl = h.HubUrl,
                    ApiKey = h.ApiKey
                })
                .ToList(),
            ChangeEventType = ChangeEventType.Updated // Mapper de nyeste URL'er fra database til DTO
        };

        return engineBaseInfo;
    }
}