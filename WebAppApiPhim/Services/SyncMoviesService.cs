using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.BackgroundServices
{
    public class SyncMoviesService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyncMoviesService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(24); // Đồng bộ mỗi 24 giờ
        private readonly TimeSpan _apiDelay = TimeSpan.FromMilliseconds(500); // Độ trễ giữa các yêu cầu API
        private readonly int _batchSize = 5; // Số lượng phim xử lý mỗi batch

        public SyncMoviesService(
            IServiceProvider serviceProvider,
            ILogger<SyncMoviesService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncMoviesService is starting.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Starting movie synchronization from API at {time}", DateTimeOffset.UtcNow);

                    try
                    {
                        await SyncMoviesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during movie synchronization from API");
                    }

                    _logger.LogInformation("Completed movie synchronization. Waiting for next run at {time}", DateTimeOffset.UtcNow.Add(_syncInterval));
                    await Task.Delay(_syncInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SyncMoviesService is stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in SyncMoviesService; service is stopping.");
            }

            _logger.LogInformation("SyncMoviesService is stopping.");
        }

        private async Task SyncMoviesAsync(CancellationToken stoppingToken)
        {
            int page = 1;
            const int limit = 10;
            bool hasMorePages = true;
            const int maxRetries = 3;
            string[] apiVersions = new[] { "v3", "v2", "v1" }; // Ưu tiên V3, V2 vì có TmdbId

            using var scope = _serviceProvider.CreateScope();
            var movieApiService = scope.ServiceProvider.GetRequiredService<IMovieApiService>();
            var streamingService = scope.ServiceProvider.GetRequiredService<IStreamingService>();
            var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            while (hasMorePages && !stoppingToken.IsCancellationRequested)
            {
                MovieListResponse movieList = null;
                int retryCount = 0;
                bool success = false;

                // Thử từng phiên bản API
                while (retryCount < maxRetries && !success && !stoppingToken.IsCancellationRequested)
                {
                    foreach (var version in apiVersions)
                    {
                        try
                        {
                            _logger.LogInformation("Fetching movies from API: page {page}, limit {limit}, version {version}", page, limit, version);
                            movieList = await movieApiService.GetLatestMoviesAsync(page, limit, version);

                            if (movieList?.Data != null && movieList.Data.Any())
                            {
                                success = true;
                                break;
                            }

                            _logger.LogWarning("No movie data received from API for page {page}, version {version}", page, version);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error fetching movies for page {page}, version {version}", page, version);
                        }
                    }

                    if (!success)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            _logger.LogError("Max retries ({maxRetries}) reached for page {page}", maxRetries, page);
                            hasMorePages = false;
                            break;
                        }
                        await Task.Delay(TimeSpan.FromSeconds(5 * retryCount), stoppingToken);
                    }
                }

                if (movieList?.Data == null || !movieList.Data.Any())
                {
                    _logger.LogWarning("No movies found for page {page}, stopping pagination", page);
                    hasMorePages = false;
                    continue;
                }

                _logger.LogInformation("Found {count} movies on page {page}/{totalPages}",
                    movieList.Data.Count, page, movieList.Pagination?.TotalPages ?? 1);

                // Xử lý phim theo batch
                for (int i = 0; i < movieList.Data.Count; i += _batchSize)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    var batch = movieList.Data.Skip(i).Take(_batchSize).ToList();

                    foreach (var movie in batch)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        await SyncMovieDetailAsync(movie, movieApiService, streamingService, statisticsService, dbContext, stoppingToken);
                        await Task.Delay(_apiDelay, stoppingToken);
                    }
                }

                hasMorePages = movieList.Pagination != null && page < movieList.Pagination.TotalPages;
                page++;
            }
        }

        private async Task SyncMovieDetailAsync(
            MovieItem movie,
            IMovieApiService movieApiService,
            IStreamingService streamingService,
            IStatisticsService statisticsService,
            ApplicationDbContext dbContext,
            CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(movie?.Slug))
            {
                _logger.LogWarning("Invalid movie slug provided for synchronization");
                return;
            }

            try
            {
                // Kiểm tra phim hiện có
                var existingMovie = await dbContext.CachedMovies
                    .AsNoTracking()
                    .Select(m => new { m.Slug, m.LastUpdated })
                    .FirstOrDefaultAsync(m => m.Slug == movie.Slug, stoppingToken);

                DateTime modifiedDate = DateTime.TryParse(movie.Modified?.Time, out var date) ? date : DateTime.MinValue;

                if (existingMovie != null && existingMovie.LastUpdated >= modifiedDate && modifiedDate != DateTime.MinValue)
                {
                    _logger.LogInformation("Movie already exists and is up-to-date: {slug}", movie.Slug);
                    return;
                }

                // Lấy chi tiết phim
                MovieDetailResponse movieDetail = null;
                string[] apiVersions = new[] { "v3", "v2", "v1" };
                foreach (var version in apiVersions)
                {
                    try
                    {
                        movieDetail = await movieApiService.GetMovieDetailBySlugAsync(movie.Slug, version);
                        if (movieDetail != null)
                            break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch movie details for {slug} with version {version}", movie.Slug, version);
                    }
                }

                if (movieDetail == null)
                {
                    _logger.LogWarning("Failed to fetch movie details for slug {slug} with all API versions", movie.Slug);
                    return;
                }

                // Sử dụng StreamingService để lưu phim
                var cachedMovie = await streamingService.CacheMovieAsync(movieDetail);

                // Làm mới bộ nhớ đệm thống kê
                statisticsService.InvalidateCache(movieDetail.Slug);

                _logger.LogInformation("Successfully synchronized movie: {slug}", movieDetail.Slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing movie: {slug}", movie.Slug);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SyncMoviesService is stopping.");
            await base.StopAsync(cancellationToken);
        }
    }
}