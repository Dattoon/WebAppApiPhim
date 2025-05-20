using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Services
{
    public class MovieCacheService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MovieCacheService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6);

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

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MovieCacheService is running at: {time}", DateTimeOffset.Now);

                try
                {
                    await UpdateMovieCacheAsync();
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

        private async Task UpdateMovieCacheAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var movieApiService = scope.ServiceProvider.GetRequiredService<IMovieApiService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                // Lấy phim mới nhất
                var latestMovies = await movieApiService.GetLatestMoviesAsync(1, 50);
                if (latestMovies?.Data == null || !latestMovies.Data.Any())
                {
                    _logger.LogWarning("No latest movies found from API");
                    return;
                }

                _logger.LogInformation("Found {count} latest movies from API", latestMovies.Data.Count);

                // Cập nhật cache
                foreach (var movie in latestMovies.Data)
                {
                    try
                    {
                        var existingMovie = await dbContext.CachedMovies
                            .FirstOrDefaultAsync(m => m.Slug == movie.Slug);

                        if (existingMovie == null)
                        {
                            _logger.LogInformation("Caching new movie: {slug}", movie.Slug);

                            // Lấy chi tiết phim
                            var movieDetail = await movieApiService.GetMovieDetailBySlugAsync(movie.Slug);
                            if (movieDetail != null)
                            {
                                // Tạo CachedMovie
                                var cachedMovie = new CachedMovie
                                {
                                    Slug = movie.Slug,
                                    Name = movie.Name,
                                    OriginalName = movieDetail.OriginalName ?? movie.Name,
                                    Description = movieDetail.Description,
                                    PosterUrl = movie.PosterUrl ?? movieDetail.Poster_url ?? movieDetail.Sub_poster,
                                    ThumbUrl = movie.ThumbUrl ?? movieDetail.Thumb_url ?? movieDetail.Sub_thumb,
                                    Year = movie.Year,
                                    Type = movie.Loai_phim,
                                    Country = movie.Quoc_gia,
                                    Genres = movieDetail.Genres,
                                    Director = movieDetail.Director ?? movieDetail.Directors,
                                    Actors = movieDetail.Casts ?? movieDetail.Actors,
                                    Duration = movieDetail.Time,
                                    Quality = movieDetail.Quality,
                                    Language = movieDetail.Language,
                                    ViewCount = 0,
                                    LastUpdated = DateTime.Now
                                };

                                dbContext.CachedMovies.Add(cachedMovie);
                                await dbContext.SaveChangesAsync();

                                // Cập nhật thống kê
                                await analyticsService.UpdateMovieStatisticsAsync(movie.Slug);
                            }
                        }
                        else
                        {
                            // Kiểm tra xem có cần cập nhật không
                            var lastUpdated = existingMovie.LastUpdated;
                            var daysSinceLastUpdate = (DateTime.Now - lastUpdated).TotalDays;

                            if (daysSinceLastUpdate > 1) // Cập nhật nếu đã hơn 1 ngày
                            {
                                _logger.LogInformation("Updating cached movie: {slug}", movie.Slug);

                                // Lấy chi tiết phim
                                var movieDetail = await movieApiService.GetMovieDetailBySlugAsync(movie.Slug);
                                if (movieDetail != null)
                                {
                                    // Cập nhật CachedMovie
                                    existingMovie.Name = movie.Name;
                                    existingMovie.OriginalName = movieDetail.OriginalName ?? movie.Name;
                                    existingMovie.Description = movieDetail.Description;
                                    existingMovie.PosterUrl = movie.PosterUrl ?? movieDetail.Poster_url ?? movieDetail.Sub_poster ?? existingMovie.PosterUrl;
                                    existingMovie.ThumbUrl = movie.ThumbUrl ?? movieDetail.Thumb_url ?? movieDetail.Sub_thumb ?? existingMovie.ThumbUrl;
                                    existingMovie.Year = movie.Year;
                                    existingMovie.Type = movie.Loai_phim;
                                    existingMovie.Country = movie.Quoc_gia;
                                    existingMovie.Genres = movieDetail.Genres;
                                    existingMovie.Director = movieDetail.Director ?? movieDetail.Directors;
                                    existingMovie.Actors = movieDetail.Casts ?? movieDetail.Actors;
                                    existingMovie.Duration = movieDetail.Time;
                                    existingMovie.Quality = movieDetail.Quality;
                                    existingMovie.Language = movieDetail.Language;
                                    existingMovie.LastUpdated = DateTime.Now;

                                    dbContext.CachedMovies.Update(existingMovie);
                                    await dbContext.SaveChangesAsync();

                                    // Cập nhật thống kê
                                    await analyticsService.UpdateMovieStatisticsAsync(movie.Slug);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error caching movie {slug}", movie.Slug);
                    }
                }

                _logger.LogInformation("Movie cache update completed");
            }
        }
    }
}