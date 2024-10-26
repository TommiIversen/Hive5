using Common.DTOs;
using Serilog;
using System.Collections.Concurrent;

namespace Engine.Services;

public interface ILoggerService
{
    void LogMessage(BaseLogEntry logEntry);
}

public class LoggerService(IEngineIdProvider engineIdProvider, MessageQueue messageQueue) : ILoggerService
{
    private readonly Guid _engineId = engineIdProvider.GetEngineId();
    private readonly ConcurrentDictionary<string, int> _workerLogCounters = new();
    private int _engineLogCounter = 0;

    public void LogMessage(BaseLogEntry logEntry)
    {
        // Sæt EngineId og Timestamp automatisk
        logEntry.EngineId = _engineId;
        logEntry.Timestamp = DateTime.UtcNow;

        // Sæt sekvensnummer afhængigt af logtypen
        if (logEntry is WorkerLogEntry workerLogEntry)
        {
            // Initialiser tælleren for denne WorkerId, hvis den ikke allerede findes
            _workerLogCounters.TryAdd(workerLogEntry.WorkerId, 0);

            // Opdater tælleren i en løkke for at sikre thread-sikker opdatering
            int currentCount;
            int newCount;
            do
            {
                currentCount = _workerLogCounters[workerLogEntry.WorkerId];
                newCount = currentCount + 1;
            }
            while (!_workerLogCounters.TryUpdate(workerLogEntry.WorkerId, newCount, currentCount));

            // Tildel det nye sekvensnummer
            workerLogEntry.LogSequenceNumber = newCount;
        }
        else if (logEntry is EngineLogEntry)
        {
            logEntry.SequenceNumber = Interlocked.Increment(ref _engineLogCounter);
        }

        messageQueue.EnqueueMessage(logEntry);
        LogToSerilog(logEntry);
    }

    private void LogToSerilog(BaseLogEntry logEntry)
    {
        var logMessage = $"{logEntry.GetType().Name} - Engine ID: {_engineId}, Message: {logEntry.Message}";

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
            case LogLevel.None:
            default:
                Log.Information(logMessage);
                break;
        }
    }
}
