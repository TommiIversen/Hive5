using System;
using System.Threading.Tasks;

namespace Engine.Commands
{
    public class StopWorkerCommand : ICommand
    {
        private readonly Guid _workerId;

        public StopWorkerCommand(Guid workerId)
        {
            _workerId = workerId;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            // Simulate stop worker logic
            Console.WriteLine($"Stopping worker {_workerId}");

            // Simulate success
            bool success = true;
            string message = success ? $"Worker {_workerId} stopped successfully." : $"Failed to stop worker {_workerId}.";

            return new CommandResult(success, message);
        }
    }
}