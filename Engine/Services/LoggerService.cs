using System.Collections.Concurrent;
using Common.DTOs;
using Serilog;

namespace Engine.Services
{
    public interface ILoggerService
    {
        void LogMessage(BaseLogEntry logEntry);
        void SetEngineId(Guid engineId); // Ny metode til dynamisk at opdatere EngineId
    }

    public class LoggerService : ILoggerService
    {
        private Guid _engineId = Guid.Empty; // Initialiseret til tom GUID
        private readonly MessageQueue _messageQueue;
        private readonly ConcurrentDictionary<string, int> _workerLogCounters = new();
        private int _engineLogCounter;

        public LoggerService(MessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public void SetEngineId(Guid engineId)
        {
            _engineId = engineId;
        }

        public void LogMessage(BaseLogEntry logEntry)
        {
            if (_engineId == Guid.Empty)
            {
                Log.Warning("EngineId er ikke sat. Logning uden EngineId.");
            }
            else
            {
                logEntry.EngineId = _engineId;
            }

            logEntry.Timestamp = DateTime.UtcNow;

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
            }
            else if (logEntry is EngineLogEntry)
            {
                logEntry.SequenceNumber = Interlocked.Increment(ref _engineLogCounter);
            }

            _messageQueue.EnqueueMessage(logEntry);
            LogToSerilog(logEntry);
        }

        private void LogToSerilog(BaseLogEntry logEntry)
        {
            var logMessage = $"{logEntry.GetType().Name} - Message: {logEntry.Message}";

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
}
