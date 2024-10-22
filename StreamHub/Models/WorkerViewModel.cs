using System.Collections.Concurrent;
using Common.Models;

namespace StreamHub.Models;

public class WorkerViewModel
{
    public required string WorkerId { get; set; }
    public required WorkerOut Worker { get; set; }
    public ConcurrentQueue<LogEntry> LogMessages { get; set; } = new();
    public string? LastImage { get; set; }
    public bool IsProcessing { get; set; }
    public string OperationResult { get; set; } = "";
    public DateTime EventProcessedTimestamp { get; set; }
    
    public void AddLogMessage(LogEntry message)
    {
        LogMessages.Enqueue(message);
        if (LogMessages.Count > 50)
        {
            LogMessages.TryDequeue(out _); // Remove the oldest message
        }
    }
}