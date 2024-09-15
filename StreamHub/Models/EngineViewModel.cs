// Models/EngineViewModel.cs
namespace StreamHub.Models;

public class EngineViewModel
{
    public Guid EngineId { get; set; }
    public Metric LastMetric { get; set; }
    public Dictionary<Guid, WorkerViewModel> Workers { get; set; } = new();
}