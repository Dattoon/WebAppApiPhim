using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.BackgroundServices
{
    public class EnhancedMovieCacheService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EnhancedMovieCacheService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(2); // Run every 2 hours
        private readonly TimeSpan _initialDelay = TimeSpan.FromMinutes(5);

        public EnhancedMovieCacheService(
            IServiceProvider serviceProvider,
            ILogger<EnhancedMovieCacheService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Enhanced Movie Cache Service is starting.");

            await Task.Delay(_initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Enhanced Movie Cache Service is running at: {time}", DateTimeOffset.Now);

                try
                {
                    await UpdateMovieCacheAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating movie cache");
                }

                _logger.LogInformation("Enhanced Movie Cache Service is waiting for next run at: {time}", DateTimeOffset.Now);
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Enhanced Movie Cache Service is stopping.");
        }

        private async Task UpdateMovieCacheAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var movieApiService = scope.ServiceProvider.GetRequiredService<IMovieApiService>();

            try
            {
                // Update latest movies cache
                await movieApiService.GetLatestMoviesAsync(1, 50);

                // Update popular genres cache
                await movieApiService.GetGenresAsync();

                // Update countries cache
                await movieApiService.GetCountriesAsync();

                _logger.LogInformation("Movie cache updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie cache");
            }
        }
    }
}
