using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Engine.DAL.Entities;

public class WorkerEvent
{
    [Key] public int EventId { get; set; }

    public required string WorkerId { get; set; } // Foreign key reference
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty; // Årsagen til hændelsen

    public List<WorkerEventLog> EventLogs { get; set; } = new(); // De sidste 20 logs ved denne hændelse
}

public class WorkerEventLog
{
    [Key] public int LogId { get; set; }

    [ForeignKey("WorkerEvent")]
    [ConcurrencyCheck] // Samtidig opdatering på fremmednøglefeltet
    public int EventId { get; set; } // Reference til WorkerEvent

    public DateTime LogTimestamp { get; set; }
    public LogLevel LogLevel { get; set; }
    public required string Message { get; set; } = string.Empty;
}