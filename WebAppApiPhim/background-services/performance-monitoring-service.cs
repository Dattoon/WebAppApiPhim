using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WebAppApiPhim.BackgroundServices
{
    public class PerformanceMonitoringService : BackgroundService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Performance Monitoring Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorPerformanceAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during performance monitoring");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task MonitorPerformanceAsync()
        {
            var process = Process.GetCurrentProcess();

            // Memory usage
            var workingSetMB = process.WorkingSet64 / (1024.0 * 1024.0);
            var privateMemoryMB = process.PrivateMemorySize64 / (1024.0 * 1024.0);

            // CPU usage (simplified)
            var cpuTime = process.TotalProcessorTime;

            // GC information
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);
            var totalMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

            _logger.LogInformation(
                "Performance Metrics - WorkingSet: {WorkingSetMB:F2}MB, PrivateMemory: {PrivateMemoryMB:F2}MB, " +
                "GC Memory: {GCMemoryMB:F2}MB, GC Collections - Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2}",
                workingSetMB, privateMemoryMB, totalMemory, gen0Collections, gen1Collections, gen2Collections);

            // Alert on high memory usage
            if (workingSetMB > 500) // 500MB threshold
            {
                _logger.LogWarning("High memory usage detected: {WorkingSetMB:F2}MB", workingSetMB);
            }
        }
    }
}
