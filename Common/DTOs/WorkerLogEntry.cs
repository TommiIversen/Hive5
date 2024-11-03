using Microsoft.Extensions.Logging;

namespace Common.DTOs;


public class BaseLogEntry : BaseMessage
{
    public required string Message { get; set; }
    public int LogSequenceNumber { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public DateTime LogTimestamp { get; set; }
}


public class WorkerLogEntry : BaseLogEntry
{
    public required string WorkerId { get; set; }
    
}

public class EngineLogEntry : BaseLogEntry
{
    
}


public class WorkerEventLogDto
{
    public DateTime EventTimestamp { get; set; }
    public string EventMessage { get; set; } = string.Empty;
    public List<WorkerLogEntry> Logs { get; set; } = new(); // Hver event har en liste af logs
}

public class WorkerEventWithLogsDto
{
    public string WorkerId { get; set; } = string.Empty;
    public List<WorkerEventLogDto> Events { get; set; } = new(); // De seneste 20 events for worker
}

