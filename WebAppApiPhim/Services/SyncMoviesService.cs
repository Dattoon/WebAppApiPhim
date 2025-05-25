using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.BackgroundServices
{
    public class SyncMoviesService : BackgroundService
    {
        private readonly ILogger<SyncMoviesService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SyncMoviesService(ILogger<SyncMoviesService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncMoviesService is starting.");

            // Wait 1 minute before first run to let the app fully start
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SafeSyncMovies(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during movie synchronization.");
                }

                // Run every 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }

            _logger.LogInformation("SyncMoviesService is stopping.");
        }

        private async Task SafeSyncMovies(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting safe movie synchronization...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var movieApiService = scope.ServiceProvider.GetRequiredService<IMovieApiService>();

            try
            {
                var totalSynced = 0;

                // Sync only 3 pages (60 movies) to be safe
                for (int page = 1; page <= 3; page++)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        var apiResult = await movieApiService.GetLatestMoviesAsync(page, 20, "v1");

                        foreach (var apiMovie in apiResult.Data)
                        {
                            try
                            {
                                // Skip if essential data is missing
                                if (string.IsNullOrWhiteSpace(apiMovie.Slug) || string.IsNullOrWhiteSpace(apiMovie.Title))
                                {
                                    continue;
                                }

                                var existingMovie = await context.CachedMovies
                                    .FirstOrDefaultAsync(m => m.Slug == apiMovie.Slug, stoppingToken);

                                if (existingMovie == null)
                                {
                                    var cachedMovie = new CachedMovie
                                    {
                                        Slug = apiMovie.Slug,
                                        Title = apiMovie.Title,
                                        PosterUrl = apiMovie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                                        LastUpdated = DateTime.UtcNow,
                                        Views = 0,

                                        // Provide safe defaults for all fields
                                        Description = "",
                                        Director = "",
                                        Duration = "",
                                        Language = "",
                                        ThumbUrl = apiMovie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                                        Year = apiMovie.Year ?? "",
                                        TmdbId = apiMovie.TmdbId ?? "",
                                        Resolution = "",
                                        TrailerUrl = "",
                                        Rating = null,
                                        RawData = ""
                                    };

                                    context.CachedMovies.Add(cachedMovie);
                                    totalSynced++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process movie: {slug}", apiMovie.Slug);
                            }
                        }

                        // Save after each page
                        if (totalSynced > 0)
                        {
                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Synced page {page}, total movies: {total}", page, totalSynced);
                        }

                        // Delay between pages
                        await Task.Delay(3000, stoppingToken);
                    }
                    catch (Exception pageEx)
                    {
                        _logger.LogError(pageEx, "Failed to sync page {page}", page);
                    }
                }

                _logger.LogInformation("Movie synchronization completed. Synced {total} new movies.", totalSynced);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during movie synchronization.");
            }
        }
    }
}
