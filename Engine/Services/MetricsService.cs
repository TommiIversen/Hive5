using System;
using System.Threading;
using System.Threading.Tasks;
using Engine.Utils;
using StreamHub.Models;

namespace Engine.Services
{
    public class MetricsService
    {
        private readonly MessageQueue _messageQueue;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);  // Metrics every 10 seconds
        private readonly CpuUsageMonitor _cpuUsageMonitor = new(); // Tilføjet CpuUsageMonitor til at måle CPU-forbrug
        private readonly MemoryUsageMonitor _memoryUsageMonitor = new(); // Tilføjet MemoryUsageMonitor til at måle RAM-forbrug

        
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
                // Hent CPU- og RAM-målinger
                var totalCpuUsage = await GetCpuUsageAsync(cancellationToken);
                var perCoreCpuUsage = await GetPerCoreCpuUsageAsync(cancellationToken);
                var currentProcessCpuUsage = _cpuUsageMonitor.GetCurrentProcessCpuUsage();

                var totalMemory = await _memoryUsageMonitor.GetTotalMemoryAsync(cancellationToken);
                var availableMemory = await _memoryUsageMonitor.GetAvailableMemoryAsync(cancellationToken);
                var currentProcessMemoryUsage = _memoryUsageMonitor.GetCurrentProcessMemoryUsage();

                // Generate and enqueue a new metric
                var metric = new Metric
                {
                    Timestamp = DateTime.UtcNow,
                    CPUUsage = totalCpuUsage, // Samlet CPU-brug
                    PerCoreCpuUsage = perCoreCpuUsage, // CPU-brug per kerne
                    CurrentProcessCpuUsage = currentProcessCpuUsage, // Nuværende proces CPU-brug
                    MemoryUsage = totalMemory - availableMemory, // Brug fiktiv hukommelsesmåling for nu
                    TotalMemory = totalMemory, // Samlet RAM i systemet
                    AvailableMemory = availableMemory, // Tilgængelig RAM i systemet
                    CurrentProcessMemoryUsage = currentProcessMemoryUsage // RAM-brug for den aktuelle proces
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

        private async Task<double> GetCpuUsageAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Brug CpuUsageMonitor til at hente samlet CPU-forbrug
                return await _cpuUsageMonitor.GetTotalCpuUsageAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error measuring CPU usage: {ex.Message}");
                return 0.0; // Returner 0 i tilfælde af fejl
            }
        }
        
        private async Task<List<double>> GetPerCoreCpuUsageAsync(CancellationToken cancellationToken)
        {
            try
            {
                var perCoreUsage = await _cpuUsageMonitor.GetPerCoreCpuUsageAsync(cancellationToken);
                return perCoreUsage.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error measuring per core CPU usage: {ex.Message}");
                return new List<double>(); // Returner tom liste i tilfælde af fejl
            }
        }

        private double GetFakeMemoryUsage()
        {
            return Random.Shared.NextDouble() * 16000; // MB
        }
    }
}
