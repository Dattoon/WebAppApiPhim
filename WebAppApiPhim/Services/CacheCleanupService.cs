using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAppApiPhim.Data;

namespace WebAppApiPhim.Services
{
    public class CacheCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Run once a day
        private readonly TimeSpan _initialDelay = TimeSpan.FromHours(1); // Wait 1 hour after startup before first run

        public CacheCleanupService(
            IServiceProvider serviceProvider,
            ILogger<CacheCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheCleanupService is starting.");

            // Initial delay
            await Task.Delay(_initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("CacheCleanupService is running at: {time}", DateTimeOffset.Now);

                try
                {
                    await CleanupCacheAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up cache");
                }

                _logger.LogInformation("CacheCleanupService is waiting for next run at: {time}", DateTimeOffset.Now);
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("CacheCleanupService is stopping.");
        }

        private async Task CleanupCacheAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get date threshold (movies older than 3 months with no views)
                var threshold = DateTime.Now.AddMonths(-3);

                // Find old movies with no views
                var oldMovies = await dbContext.CachedMovies
                    .Where(m => m.LastUpdated < threshold && m.ViewCount == 0)
                    .ToListAsync(stoppingToken);

                if (oldMovies.Any())
                {
                    _logger.LogInformation("Found {count} old unused movies to clean up", oldMovies.Count);

                    // Delete old movies
                    dbContext.CachedMovies.RemoveRange(oldMovies);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                // Clean up old episode progress (older than 6 months)
                var progressThreshold = DateTime.Now.AddMonths(-6);
                var oldProgress = await dbContext.EpisodeProgresses
                    .Where(p => p.UpdatedAt < progressThreshold)
                    .ToListAsync(stoppingToken);

                if (oldProgress.Any())
                {
                    _logger.LogInformation("Found {count} old episode progress records to clean up", oldProgress.Count);

                    dbContext.EpisodeProgresses.RemoveRange(oldProgress);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                _logger.LogInformation("Cache cleanup completed");
            }
        }
    }
}
