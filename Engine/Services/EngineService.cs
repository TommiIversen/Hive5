using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Database;
using Microsoft.EntityFrameworkCore;

namespace Engine.Services;

public interface IEngineService
{
    Task<EngineEntities?> GetEngineAsync();
    Task UpdateEngineAsync(string name, string description);
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
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        await _engineRepository.UpdateEngineAsync(name, description);
    }
}