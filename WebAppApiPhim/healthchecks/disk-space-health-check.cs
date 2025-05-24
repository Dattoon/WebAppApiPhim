using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace WebAppApiPhim.HealthChecks
{
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        private readonly ILogger<DiskSpaceHealthCheck> _logger;
        private readonly long _minimumFreeBytesThreshold;

        public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Default to 1GB minimum free space
            _minimumFreeBytesThreshold = configuration.GetValue<long>("HealthChecks:DiskSpace:MinimumFreeBytes", 1024L * 1024L * 1024L);
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

                var results = new List<string>();
                var hasUnhealthyDrive = false;

                foreach (var drive in drives)
                {
                    var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    var totalSpaceGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    var usagePercentage = ((totalSpaceGB - freeSpaceGB) / totalSpaceGB) * 100;

                    results.Add($"Drive {drive.Name}: {freeSpaceGB:F2}GB free of {totalSpaceGB:F2}GB ({usagePercentage:F1}% used)");

                    if (drive.AvailableFreeSpace < _minimumFreeBytesThreshold)
                    {
                        hasUnhealthyDrive = true;
                    }
                }

                var message = string.Join("; ", results);

                if (hasUnhealthyDrive)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy($"Low disk space detected. {message}"));
                }

                return Task.FromResult(HealthCheckResult.Healthy($"Disk space is healthy. {message}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk space health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy("Disk space health check failed", ex));
            }
        }
    }
}
