using WebAppApiPhim.Services;

namespace WebAppApiPhim.BackgroundServices
{
    public class EpisodeSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EpisodeSyncBackgroundService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6); // Sync every 6 hours

        public EpisodeSyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EpisodeSyncBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EpisodeSyncBackgroundService is starting.");

            // Wait 5 minutes after startup before first sync
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncRecentMovieEpisodes();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during episode sync");
                }

                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        private async Task SyncRecentMovieEpisodes()
        {
            using var scope = _serviceProvider.CreateScope();
            var episodeSyncService = scope.ServiceProvider.GetRequiredService<IEpisodeSyncService>();

            _logger.LogInformation("Starting automatic episode sync for recent movies...");

            try
            {
                var (added, updated, failed) = await episodeSyncService.SyncEpisodesForRecentMoviesAsync(20);
                _logger.LogInformation("Episode sync completed. Added: {added}, Updated: {updated}, Failed: {failed}",
                    added, updated, failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic episode sync");
            }
        }
    }
}
