using System;
using System.Threading.Tasks;
using Common.Models;

namespace Engine.Commands
{
    public class StartWorkerCommand : ICommand
    {
        private readonly Guid _workerId;

        public StartWorkerCommand(Guid workerId)
        {
            _workerId = workerId;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            // Simulate start worker logic
            Console.WriteLine($"Starting worker {_workerId}");

            // Simulate success
            bool success = true;
            string message = success ? $"Worker {_workerId} started successfully." : $"Failed to start worker {_workerId}.";

            return new CommandResult(success, message);
        }
    }
}