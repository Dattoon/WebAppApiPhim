using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Data;
using WebAppApiPhim.Repositories;

namespace WebAppApiPhim.Services
{
    public class MovieCacheService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MovieCacheService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6);
        private readonly TimeSpan _initialDelay = TimeSpan.FromMinutes(5); // Wait 5 minutes after startup before first run

        public MovieCacheService(
            IServiceProvider serviceProvider,
            ILogger<MovieCacheService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MovieCacheService is starting.");

            // Initial delay to avoid overloading the system at startup
            await Task.Delay(_initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MovieCacheService is running at: {time}", DateTimeOffset.Now);

                try
                {
                    await UpdateMovieCacheAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating movie cache");
                }

                _logger.LogInformation("MovieCacheService is waiting for next run at: {time}", DateTimeOffset.Now);
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("MovieCacheService is stopping.");
        }

        private async Task UpdateMovieCacheAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var movieApiService = scope.ServiceProvider.GetRequiredService<IMovieApiService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var metadataRepository = scope.ServiceProvider.GetRequiredService<IMetadataRepository>();

                // Get latest movies
                var latestMovies = await movieApiService.GetLatestMoviesAsync(1, 50);
                if (latestMovies?.Data == null || !latestMovies.Data.Any())
                {
                    _logger.LogWarning("No latest movies found from API");
                    return;
                }

                _logger.LogInformation("Found {count} latest movies from API", latestMovies.Data.Count);

                // Process movies in batches to avoid overwhelming the system
                const int batchSize = 5;
                for (int i = 0; i < latestMovies.Data.Count; i += batchSize)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    var batch = latestMovies.Data.Skip(i).Take(batchSize).ToList();

                    foreach (var movie in batch)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        try
                        {
                            var existingMovie = await dbContext.CachedMovies
                                .FirstOrDefaultAsync(m => m.Slug == movie.Slug, stoppingToken);

                            // Check if movie needs updating (not in cache or older than 1 day)
                            bool needsUpdate = existingMovie == null ||
                                (DateTime.Now - existingMovie.LastUpdated).TotalDays > 1;

                            if (needsUpdate)
                            {
                                _logger.LogInformation("Caching movie: {slug}", movie.Slug);

                                // Get movie details
                                var movieDetail = await movieApiService.GetMovieDetailBySlugAsync(movie.Slug);

                                // Wait a bit to avoid overwhelming the API
                                await Task.Delay(500, stoppingToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error caching movie {slug}", movie.Slug);
                        }
                    }
                }

                // Update metadata counts
                try
                {
                    await metadataRepository.UpdateMovieCountsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating metadata counts");
                }

                _logger.LogInformation("Movie cache update completed");
            }
        }
    }
}
