using System.Collections.Concurrent;
using Common.Models;

namespace StreamHub.Models;

public class WorkerViewModel
{
    public required Guid WorkerId { get; set; }
    
    public WorkerOut Worker { get; set; }
    public ConcurrentQueue<LogEntry> LogMessages { get; set; } = new();
    public string? LastImage { get; set; }
    
    public bool IsProcessing { get; set; }
    
    public string? OperationResult { get; set; }
    public DateTime EventProcessedTimestamp { get; set; }

    

    // Adds a new log message and ensures the queue has a max of 10 entries
    public void AddLogMessage(LogEntry message)
    {
        LogMessages.Enqueue(message);
        if (LogMessages.Count > 10)
        {
            LogMessages.TryDequeue(out _); // Remove the oldest message
        }
    }

}