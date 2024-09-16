using System;
using System.Collections.Generic;
using Engine.Models;

namespace Engine.Services
{
    public class WorkerManager
    {
        private readonly MessageQueue _messageQueue;
        private readonly Dictionary<Guid, Worker> _workers = new();

        public WorkerManager(MessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public IReadOnlyDictionary<Guid, Worker> Workers => _workers;

        public Worker AddWorker()
        {
            var worker = new Worker(_messageQueue);
            _workers[worker.WorkerId] = worker;
            return worker;
        }

        public void StartWorker(Guid workerId)
        {
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