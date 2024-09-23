// Models/WorkerViewModel.cs
namespace StreamHub.Models;

public class WorkerViewModel
{
    public Guid WorkerId { get; set; }
    public List<string> LogMessages { get; set; } = new List<string>(); 
    public string? LastImage { get; set; }
    
    public bool IsProcessing { get; set; }
    
    public string? OperationResult { get; set; }

    // Adds a new log message and ensures the list has a max of 10 entries
    public void AddLogMessage(string message)
    {
        LogMessages.Add(message);
        if (LogMessages.Count > 10)
        {
            LogMessages.RemoveAt(0); // Remove the oldest message
        }
    }
}
