using System.Collections.Concurrent;

namespace StreamHub.Models;

public class WorkerViewModel
{
    public required Guid WorkerId { get; set; }
    public ConcurrentQueue<string> LogMessages { get; set; } = new();
    public string? LastImage { get; set; }
    
    public bool IsProcessing { get; set; }
    
    public string? OperationResult { get; set; }

    // Adds a new log message and ensures the queue has a max of 10 entries
    public void AddLogMessage(string message)
    {
        LogMessages.Enqueue(message);
        if (LogMessages.Count > 10)
        {
            LogMessages.TryDequeue(out _); // Remove the oldest message
        }
    }

}