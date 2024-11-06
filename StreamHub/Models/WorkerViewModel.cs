using System.Collections.Concurrent;
using Common.DTOs;
using Common.DTOs.Events;

namespace StreamHub.Models;

public class WorkerViewModel
{
    public required string WorkerId { get; set; }
    public required WorkerInfo Worker { get; set; }
    public ConcurrentQueue<WorkerLogEntry> LogMessages { get; set; } = new();
    public string? LastImage { get; set; }
    public bool IsProcessing { get; set; }
    public string OperationResult { get; set; } = "";
    public DateTime EventProcessedTimestamp { get; set; }

    public void AddLogMessage(WorkerLogEntry message)
    {
        LogMessages.Enqueue(message);
        if (LogMessages.Count > 50) LogMessages.TryDequeue(out _); // Remove the oldest message
    }

    public void ClearWorkerLogs()
    {
        LogMessages = new ConcurrentQueue<WorkerLogEntry>();
    }
}