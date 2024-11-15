using System.Reflection;
using Common.DTOs;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.Attributes;
using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Database;
using Engine.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Engine.Services;

public interface IEngineService
{
    Task<EngineEntities?> GetEngineAsync();
    Task UpdateEngineAsync(string name, string description);
    Task AddHubUrlAsync(string hubUrl, string apiKey);
    Task RemoveHubUrlAsync(int hubUrlId);
    Task EditHubUrlAsync(int hubUrlId, string newHubUrl, string newApiKey);
    Task<EngineChangeEvent> GetEngineBaseInfoAsEvent();
}

public class EngineService : IEngineService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    //private readonly IEngineRepository _engineRepository;
    private readonly IRepositoryFactory _repositoryFactory;

    private readonly DateTime _initDateTime = DateTime.UtcNow;

    public EngineService(IRepositoryFactory repositoryFactory)
    {
        //_contextFactory = contextFactory;
        //_engineRepository = new EngineRepository(_contextFactory.CreateDbContext());
        
        _repositoryFactory = repositoryFactory;
    }

    public async Task<EngineEntities?> GetEngineAsync()
    {
        var engineRepository = _repositoryFactory.CreateEngineRepository();
        return await engineRepository.GetEngineAsync();
    }

    public async Task UpdateEngineAsync(string name, string description)
    {
        var engineRepository = _repositoryFactory.CreateEngineRepository();
        var engine = await engineRepository.GetEngineAsync();
        if (engine != null)
        {
            engine.Name = name;
            engine.Description = description;
            await engineRepository.SaveEngineAsync(engine);
        }
    }

    public async Task AddHubUrlAsync(string hubUrl, string apiKey)
    {
        var engineRepository = _repositoryFactory.CreateEngineRepository();
        var engine = await engineRepository.GetEngineAsync();
        if (engine != null)
        {
            engine.HubUrls.Add(new HubUrlEntity { HubUrl = hubUrl, ApiKey = apiKey });
            await engineRepository.SaveEngineAsync(engine);
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
        var engineRepository = _repositoryFactory.CreateEngineRepository();
        var engine = await engineRepository.GetEngineAsync();
        var hubUrl = engine?.HubUrls.FirstOrDefault(h => h.Id == hubUrlId);
        if (hubUrl != null)
        {
            hubUrl.HubUrl = newHubUrl;
            hubUrl.ApiKey = newApiKey;
            if (engine != null) await engineRepository.SaveEngineAsync(engine);
        }
    }

    public async Task<EngineChangeEvent> GetEngineBaseInfoAsEvent()
    {
        var engine = await GetEngineAsync();
        if (engine == null) throw new InvalidOperationException("Engine not found");

        var engineBaseInfo = new EngineChangeEvent
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
            ChangeEventType = ChangeEventType.Updated,
            Streamers = StreamerServiceHelper.GetStreamerNamesCached()
        };
        return engineBaseInfo;
    }

    private static List<string> GetStreamerNames()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IStreamerService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t =>
                t.GetCustomAttribute<FriendlyNameAttribute>()?.Name ?? t.Name) // Use friendly name if available
            .ToList();
    }
}