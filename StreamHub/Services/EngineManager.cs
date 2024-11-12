using System.Collections.Concurrent;
using Common.DTOs;
using Common.DTOs.Events;
using StreamHub.Models;

namespace StreamHub.Services;

public interface IEngineManager
{
    EngineViewModel GetOrAddEngine(BaseEngineInfo info);
    void UpdateBaseInfo(BaseEngineInfo info);
    void AddOrUpdateWorker(BaseWorkerInfo baseWorkerInfo);
    bool TryGetEngine(Guid engineId, out EngineViewModel? engineInfo);
    bool RemoveConnection(string connectionId);
    WorkerViewModel? GetWorker(Guid engineId, string workerId);
    IEnumerable<EngineViewModel> GetAllEngines();
    bool RemoveEngine(Guid engineId);
    void SynchronizeWorkers(List<WorkerChangeEvent> workers, Guid engineId);
    void RemoveWorker(Guid engineId, string workerId);
}

public class EngineManager(ILogger<EngineManager> logger) : IEngineManager
{
    private readonly ConcurrentDictionary<Guid, EngineViewModel> _engines = new();


    public EngineViewModel GetOrAddEngine(BaseEngineInfo info)
    {
        return _engines.GetOrAdd(info.EngineId, _ => new EngineViewModel { Info = info });
    }

    public void UpdateBaseInfo(BaseEngineInfo info)
    {
        logger.LogInformation($"Updating base info for engine {info.EngineId}");
        if (_engines.TryGetValue(info.EngineId, out var engine))
        {
            logger.LogInformation($"Base info for engine {info.EngineId} updated.");
            engine.Info = info;
            engine.Info.HubUrls = new List<HubUrlInfo>(info.HubUrls);
        }
    }

    public void AddOrUpdateWorker(BaseWorkerInfo baseWorkerInfo)
    {
        if (_engines.TryGetValue(baseWorkerInfo.EngineId, out var engine))
        {
            if (engine.Workers.TryGetValue(baseWorkerInfo.WorkerId, out var existingWorker))
            {
                if (IsOutdatedEvent(existingWorker, baseWorkerInfo))
                {
                    logger.LogInformation(
                        $"AddOrUpdateWorker: Skipping outdated event for worker {baseWorkerInfo.WorkerId} - {baseWorkerInfo.Name}");
                    return;
                }

                existingWorker.BaseWorker = baseWorkerInfo;
                existingWorker.EventProcessedTimestamp = baseWorkerInfo.Timestamp;
                engine.UpdateWorker(existingWorker);
                logger.LogInformation($"AddOrUpdateWorker: Worker {baseWorkerInfo.WorkerId} updated.");
            }
            else
            {
                var newWorker = new WorkerViewModel
                {
                    WorkerId = baseWorkerInfo.WorkerId,
                    BaseWorker = baseWorkerInfo,
                    EventProcessedTimestamp = baseWorkerInfo.Timestamp
                };
                engine.AddWorker(newWorker);
                logger.LogInformation($"AddOrUpdateWorker: Worker {baseWorkerInfo.WorkerId} added.");
            }
        }
        else
        {
            logger.LogWarning($"AddOrUpdateWorker: Engine {baseWorkerInfo.EngineId} not found. Cannot add or update worker.");
        }
    }

    private bool IsOutdatedEvent(WorkerViewModel workerViewModel, BaseWorkerInfo baseWorkerInfo)
    {
        return workerViewModel.EventProcessedTimestamp >= baseWorkerInfo.Timestamp;
    }

    public bool TryGetEngine(Guid engineId, out EngineViewModel? engineInfo)
    {
        return _engines.TryGetValue(engineId, out engineInfo);
    }


    public bool RemoveConnection(string connectionId)
    {
        var engine = _engines.Values.FirstOrDefault(e => e.ConnectionInfo.ConnectionId == connectionId);
        if (engine == null) return false;
        logger.LogInformation($"EngineManager: Removing connection {connectionId} from engine {engine.Info.EngineId} {engine.Info.EngineName}");
        engine.ConnectionInfo.ConnectionId = "";
        return true;
    }

    public WorkerViewModel? GetWorker(Guid engineId, string workerId)
    {
        if (!_engines.TryGetValue(engineId, out var engineInfo)) return null;
        engineInfo.Workers.TryGetValue(workerId, out var worker);
        return worker;
    }

    public IEnumerable<EngineViewModel> GetAllEngines()
    {
        return _engines.Values;
    }

    public bool RemoveEngine(Guid engineId)
    {
        return _engines.TryRemove(engineId, out _);
    }

    public void SynchronizeWorkers(List<WorkerChangeEvent> workers, Guid engineId)
    {
        logger.LogInformation($"SynchronizeWorkers workers: {workers.Count}");

        // Få engine fra EngineManager
        if (!_engines.TryGetValue(engineId, out var engine))
        {
            logger.LogWarning($"Engine {engineId} not found, cannot synchronize workers.");
            return;
        }

        // Liste over eksisterende workers i hukommelsen for denne engine
        var existingWorkers = engine.Workers.Keys.ToList();

        // Opdater eksisterende workers og tilføj nye
        foreach (var worker in workers)
        {
            AddOrUpdateWorker(worker);
            logger.LogInformation($"Added/Updated Worker: {worker.Name} {worker.IsEnabled}");

            // Fjern denne workerId fra listen over eksisterende workers
            existingWorkers.Remove(worker.WorkerId);
        }

        // Fjern workers, som er i hukommelsen, men ikke længere findes på den modtagne liste
        foreach (var workerId in existingWorkers)
        {
            logger.LogInformation($"Removing outdated Worker: {workerId}");
            RemoveWorker(engineId, workerId);
        }

        logger.LogInformation("Workers synchronized successfully.");
    }


    public void RemoveWorker(Guid engineId, string workerId)
    {
        if (_engines.TryGetValue(engineId, out var engine))
        {
            engine.RemoveWorker(workerId);
            logger.LogInformation($"Worker {workerId} successfully removed from engine {engineId}.");
        }
        else
        {
            logger.LogWarning($"Engine {engineId} not found. Cannot remove worker {workerId}.");
        }
    }
}