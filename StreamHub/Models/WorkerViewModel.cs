// Models/WorkerViewModel.cs
namespace StreamHub.Models;

public class WorkerViewModel
{
    public Guid WorkerId { get; set; }
    public string LastLogMessage { get; set; }
    public string LastImage { get; set; }
}