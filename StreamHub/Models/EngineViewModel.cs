using System.Collections.Concurrent;
using Common.DTOs;
using Common.DTOs.Events;

namespace StreamHub.Models;

public class ConnectionInfo
{
    public string? ConnectionId { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public string? TransportType { get; set; }
    public int LocalPort { get; set; }
    public DateTime? OnlineSince { get; set; }
    public TimeSpan? Uptime => OnlineSince.HasValue ? DateTime.UtcNow - OnlineSince.Value : null;
}

public class EngineViewModel
{
    public required BaseEngineInfo Info { get; set; }
    public EngineSystemInfoModel? SystemInfo { get; set; }
    public EngineMetric? LastMetric { get; set; }
    public ConcurrentDictionary<string, WorkerViewModel> Workers { get; } = new();
    public ConcurrentQueue<EngineLogEntry> EngineLogMessages { get; set; } = new();
    public ConnectionInfo ConnectionInfo { get; set; } = new();
    public ConcurrentQueue<MetricSimpleViewModel> MetricsQueue { get; set; } = new();

    public WorkerViewModel? HeadWorker { get; private set; }


    public bool AddWorkerLog(string workerId, WorkerLogEntry message)
    {
        if (!Workers.TryGetValue(workerId, out var worker)) return false;

        worker.AddLogMessage(message);
        return true;
    }

    public void AddWorker(WorkerViewModel newWorker)
    {
        // Hvis listen er tom, sæt den nye worker som Head
        if (HeadWorker == null)
        {
            HeadWorker = newWorker;
        }
        else
        {
            // Gå til slutningen af listen for at tilføje den nye worker
            var current = HeadWorker;
            while (current.NextWorker != null) current = current.NextWorker;

            current.NextWorker = newWorker;
            newWorker.PreviousWorker = current;
        }

        Workers[newWorker.WorkerId] = newWorker;
    }

    public void ReorderWorkers(string draggedWorkerId, string targetWorkerId)
    {
        if (!Workers.TryGetValue(draggedWorkerId, out var draggedWorker) ||
            !Workers.TryGetValue(targetWorkerId, out var targetWorker))
        {
            Console.WriteLine("Invalid worker IDs for reordering.");
            return;
        }

        // Fjern draggedWorker fra sin nuværende position
        if (draggedWorker.PreviousWorker != null) draggedWorker.PreviousWorker.NextWorker = draggedWorker.NextWorker;
        if (draggedWorker.NextWorker != null) draggedWorker.NextWorker.PreviousWorker = draggedWorker.PreviousWorker;

        // Hvis draggedWorker var HeadWorker, opdater Head
        if (HeadWorker == draggedWorker) HeadWorker = draggedWorker.NextWorker;

        // Indsæt draggedWorker før targetWorker
        draggedWorker.NextWorker = targetWorker;
        draggedWorker.PreviousWorker = targetWorker.PreviousWorker;

        if (targetWorker.PreviousWorker != null)
            targetWorker.PreviousWorker.NextWorker = draggedWorker;
        else
            // Hvis targetWorker var HeadWorker, gør draggedWorker til ny Head
            HeadWorker = draggedWorker;

        targetWorker.PreviousWorker = draggedWorker;
    }

    public IEnumerable<WorkerViewModel> GetOrderedWorkers()
    {
        var current = HeadWorker; // Antag at HeadWorker er starten af din linked list
        while (current != null)
        {
            yield return current;
            current = current.NextWorker;
        }
    }

    public void UpdateWorker(WorkerViewModel updatedWorker)
    {
        if (Workers.ContainsKey(updatedWorker.WorkerId)) Workers[updatedWorker.WorkerId] = updatedWorker;
    }

    public void RemoveWorker(string workerId)
    {
        if (Workers.TryGetValue(workerId, out var workerToRemove))
        {
            // Opdater referencer for at fjerne worker fra linked list
            if (workerToRemove.PreviousWorker != null)
                workerToRemove.PreviousWorker.NextWorker = workerToRemove.NextWorker;
            if (workerToRemove.NextWorker != null)
                workerToRemove.NextWorker.PreviousWorker = workerToRemove.PreviousWorker;
            // Hvis det er den første worker (Head), opdater Head
            if (workerToRemove == HeadWorker) HeadWorker = workerToRemove.NextWorker;

            Workers.Remove(workerId, out _);
        }
    }

    public void AddEngineLog(EngineLogEntry message)
    {
        EngineLogMessages.Enqueue(message);
        if (EngineLogMessages.Count > 50) EngineLogMessages.TryDequeue(out _); // Remove the oldest message
    }

    public void ClearEngineLogs()
    {
        EngineLogMessages = new ConcurrentQueue<EngineLogEntry>();
    }

    public void AddMetric(EngineMetric engineMetric)
    {
        var simplifiedMetric = new MetricSimpleViewModel(engineMetric);
        MetricsQueue.Enqueue(simplifiedMetric);
        if (MetricsQueue.Count > 20) MetricsQueue.TryDequeue(out _);
        LastMetric = engineMetric;
    }
}