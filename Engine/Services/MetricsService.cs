using System;
using System.Threading;
using System.Threading.Tasks;
using StreamHub.Models;

namespace Engine.Services
{
    public class MetricsService
    {
        private readonly MessageQueue _messageQueue;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);  // Metrics every 10 seconds

        public MetricsService(MessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        // Start the metrics loop
        public void Start()
        {
            _ = Task.Run(async () => await GenerateMetricsAsync(_cancellationTokenSource.Token));
        }

        // Asynchronous loop that generates metrics every 10 seconds
        private async Task GenerateMetricsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Generate and enqueue a new metric
                var metric = new Metric
                {
                    Timestamp = DateTime.UtcNow,
                    CPUUsage = GetFakeCPUUsage(),
                    MemoryUsage = GetFakeMemoryUsage()
                };

                Console.WriteLine($"Generated metric: {metric.CPUUsage}% CPU, {metric.MemoryUsage} MB memory");
                _messageQueue.EnqueueMessage(metric);

                // Wait for the next interval or exit if canceled
                try
                {
                    await Task.Delay(_interval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Metrics generation task was cancelled.");
                    break;
                }
            }
        }

        // Cancel the task if needed
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private double GetFakeCPUUsage()
        {
            return Random.Shared.NextDouble() * 100;
        }

        private double GetFakeMemoryUsage()
        {
            return Random.Shared.NextDouble() * 16000; // MB
        }
    }
}
