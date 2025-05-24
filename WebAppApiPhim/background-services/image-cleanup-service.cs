using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebAppApiPhim.BackgroundServices
{
    public class ImageCleanupService : BackgroundService
    {
        private readonly ILogger<ImageCleanupService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Run daily

        public ImageCleanupService(
            ILogger<ImageCleanupService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Image Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldImagesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during image cleanup");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CleanupOldImagesAsync()
        {
            var cacheDirectory = _configuration["ImageCache:Directory"] ?? "wwwroot/cache/images";

            if (!Directory.Exists(cacheDirectory))
                return;

            var cutoffDate = DateTime.Now.AddDays(-7); // Delete images older than 7 days
            var files = Directory.GetFiles(cacheDirectory, "*.*", SearchOption.AllDirectories);

            var deletedCount = 0;
            var totalSize = 0L;

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastAccessTime < cutoffDate)
                    {
                        totalSize += fileInfo.Length;
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {File}", file);
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old images, freed {Size:F2} MB",
                    deletedCount, totalSize / (1024.0 * 1024.0));
            }
        }
    }
}
