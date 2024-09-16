using System;
using System.Collections.Generic;
using Engine.Models;
using Serilog;

namespace Engine.Services
{
    public class WorkerManager
    {
        private readonly MessageQueue _messageQueue;
        private readonly Dictionary<Guid, WorkerService> _workers = new();

        public WorkerManager(MessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public IReadOnlyDictionary<Guid, WorkerService> Workers => _workers;

        public WorkerService AddWorker()
        {
            Log.Information($"Adding worker...");
            var worker = new WorkerService(_messageQueue);
            _workers[worker.WorkerId] = worker;
            return worker;
        }

        public void StartWorker(Guid workerId)
        {
            Log.Information($"Starting worker: {workerId}");
            if (_workers.TryGetValue(workerId, out var worker))
            {
                worker.Start();
            }
        }

        public void StopWorker(Guid workerId)
        {
            if (_workers.TryGetValue(workerId, out var worker))
            {
                worker.Stop();
            }
        }

        public void RemoveWorker(Guid workerId)
        {
            _workers.Remove(workerId);
        }
    }
}