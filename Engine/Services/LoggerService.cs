using System.Collections.Concurrent;
using Common.DTOs;
using Engine.Interfaces;
using ILogger = Serilog.ILogger;

namespace Engine.Services;

public interface ILoggerService
{
    void LogMessage(BaseLogEntry logEntry);
    void SetEngineId(Guid engineId);
    IEnumerable<BaseLogEntry> GetLastWorkerLogs(string workerId);
    void DeleteWorkerLogs(string workerId);
}

public class LoggerService : ILoggerService
{
    private readonly ILogger _logger;
    private readonly IMessageQueue _messageQueue;
    private readonly ConcurrentDictionary<string, int> _workerLogCounters = new();

    // Log-cache for de sidste 20 logs pr. worker
    private readonly ConcurrentDictionary<string, ConcurrentQueue<BaseLogEntry>> _workerLogs = new();
    private Guid _engineId = Guid.Empty;
    private int _engineLogCounter;

    public LoggerService(IMessageQueue messageQueue, ILogger logger)
    {
        _messageQueue = messageQueue;
        _logger = logger;
    }

    public void SetEngineId(Guid engineId)
    {
        _engineId = engineId;
    }

    public void LogMessage(BaseLogEntry logEntry)
    {
        if (_engineId == Guid.Empty)
            _logger.Warning("EngineId er ikke sat. Logning uden EngineId.");
        else
            logEntry.EngineId = _engineId;

        //logEntry.Timestamp = DateTime.UtcNow;
        //logEntry.LogTimestamp = DateTime.UtcNow;

        if (logEntry is WorkerLogEntry workerLogEntry)
        {
            _workerLogCounters.TryAdd(workerLogEntry.WorkerId, 0);

            int currentCount;
            int newCount;
            do
            {
                currentCount = _workerLogCounters[workerLogEntry.WorkerId];
                newCount = currentCount + 1;
            } while (!_workerLogCounters.TryUpdate(workerLogEntry.WorkerId, newCount, currentCount));

            workerLogEntry.LogSequenceNumber = newCount;

            // Tilføj loggen til worker-log-cachen
            var logQueue = _workerLogs.GetOrAdd(workerLogEntry.WorkerId, new ConcurrentQueue<BaseLogEntry>());
            logQueue.Enqueue(workerLogEntry);

            // Bevar kun de sidste 20 logs
            while (logQueue.Count > 20) logQueue.TryDequeue(out _);
        }
        else if (logEntry is EngineLogEntry)
        {
            logEntry.SequenceNumber = Interlocked.Increment(ref _engineLogCounter);
        }

        _messageQueue.EnqueueMessage(logEntry);
        LogToSerilog(logEntry);
    }

    public IEnumerable<BaseLogEntry> GetLastWorkerLogs(string workerId)
    {
        if (_workerLogs.TryGetValue(workerId, out var logQueue)) return logQueue.ToArray();

        return Array.Empty<BaseLogEntry>();
    }

    public void DeleteWorkerLogs(string workerId)
    {
        _workerLogCounters.TryRemove(workerId, out _);
        _workerLogs.TryRemove(workerId, out _);
    }

    private void LogToSerilog(BaseLogEntry logEntry)
    {
        var logMessage = $"{logEntry.GetType().Name} - Message: {logEntry.Message}";

        switch (logEntry.LogLevel)
        {
            case LogLevel.Trace:
                _logger.Verbose(logMessage);
                break;
            case LogLevel.Debug:
                _logger.Debug(logMessage);
                break;
            case LogLevel.Information:
                _logger.Information(logMessage);
                break;
            case LogLevel.Warning:
                _logger.Warning(logMessage);
                break;
            case LogLevel.Error:
                _logger.Error(logMessage);
                break;
            case LogLevel.Critical:
                _logger.Fatal(logMessage);
                break;
            case LogLevel.None:
            default:
                _logger.Information(logMessage);
                break;
        }
    }
}