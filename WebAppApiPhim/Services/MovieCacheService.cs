using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
    public class MovieCacheService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MovieCacheService> _logger;

        public MovieCacheService(IServiceProvider serviceProvider, ILogger<MovieCacheService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MovieCacheService is running at: {Time}", DateTime.UtcNow);
                await CacheMoviesAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
            }
        }

        private async Task CacheMoviesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var movieApiService = scope.ServiceProvider.GetRequiredService<IMovieApiService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var distributedCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MovieCacheService>>();

            try
            {
                var page = 1;
                while (true)
                {
                    var moviesResponse = await movieApiService.GetMoviesByCategoryAsync("all", page);
                    if (moviesResponse.Data == null || !moviesResponse.Data.Any())
                    {
                        logger.LogInformation("No more movies to cache at page {Page}", page);
                        break;
                    }

                    foreach (var movie in moviesResponse.Data)
                    {
                        var cacheKey = $"movie_{movie.Slug}";
                        var cachedMovie = new CachedMovie
                        {
                            Slug = movie.Slug,
                            Title = movie.Title,
                            LastUpdated = DateTime.UtcNow
                        };

                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                        };

                        var json = JsonSerializer.Serialize(cachedMovie);
                        await distributedCache.SetStringAsync(cacheKey, json, cacheOptions);
                        logger.LogInformation("Cached movie: {Slug}", movie.Slug);
                    }

                    page++;
                    if (moviesResponse.Pagination?.TotalPages > 0 && page > moviesResponse.Pagination.TotalPages)
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during movie caching");
            }
        }
    }
}
