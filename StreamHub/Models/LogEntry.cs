﻿// Models/LogEntry.cs
namespace StreamHub.Models;

public class LogEntry
{
    public Guid EngineId { get; set; }
    public Guid WorkerId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }
}