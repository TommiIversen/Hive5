using Common.DTOs;
using Serilog;

namespace Engine.Services;

public class LoggerService(Guid engineId, MessageQueue messageQueue)
{
    private int _workerLogCounter = 0;
    private int _engineLogCounter = 0;

    public void LogMessage(BaseLogEntry logEntry)
    {
        // Sæt EngineId og Timestamp automatisk
        logEntry.EngineId = engineId;
        logEntry.Timestamp = DateTime.UtcNow;

        // Sæt sekvensnummer afhængigt af logtypen
        if (logEntry is WorkerLogEntry)
        {
            Console.WriteLine("ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ WorkerLogEntry");
            Console.WriteLine($"logEntry.EngineId: {logEntry.EngineId}");
            logEntry.LogSequenceNumber = Interlocked.Increment(ref _workerLogCounter);
        }
        else if (logEntry is EngineLogEntry)
        {
            logEntry.SequenceNumber = Interlocked.Increment(ref _engineLogCounter);
        }

        // Tilføj til køen
        messageQueue.EnqueueMessage(logEntry);

        // Log til Serilog baseret på logniveau
        LogToSerilog(logEntry);
    }

    private void LogToSerilog(BaseLogEntry logEntry)
    {
        string logMessage = $"{logEntry.GetType().Name} - Engine ID: {engineId}, Message: {logEntry.Message}";

        switch (logEntry.LogLevel)
        {
            case LogLevel.Trace:
                Log.Verbose(logMessage);
                break;
            case LogLevel.Debug:
                Log.Debug(logMessage);
                break;
            case LogLevel.Information:
                Log.Information(logMessage);
                break;
            case LogLevel.Warning:
                Log.Warning(logMessage);
                break;
            case LogLevel.Error:
                Log.Error(logMessage);
                break;
            case LogLevel.Critical:
                Log.Fatal(logMessage);
                break;
            default:
                Log.Information(logMessage);
                break;
        }
    }
}
