using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IStatisticsService
    {
        Task<StatisticsViewModel> GetStatisticsAsync();
        Task<List<MovieListItemViewModel>> GetPopularMoviesAsync(int count = 10);
        Task<List<MovieListItemViewModel>> GetRecentMoviesAsync(int count = 10);
        Task<int> GetTotalViewsAsync();
        Task<int> GetTodayViewsAsync();
    }

    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StatisticsService> _logger;
        private readonly TimeSpan _cacheTime = TimeSpan.FromHours(1);

        public StatisticsService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<StatisticsService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<StatisticsViewModel> GetStatisticsAsync()
        {
            string cacheKey = "site_statistics";

            if (_cache.TryGetValue(cacheKey, out StatisticsViewModel cachedStats))
            {
                return cachedStats;
            }

            try
            {
                var stats = new StatisticsViewModel
                {
                    TotalMovies = await _context.CachedMovies.CountAsync(),
                    TotalEpisodes = await _context.CachedEpisodes.CountAsync(),
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalViews = await GetTotalViewsAsync(),
                    TodayViews = await GetTodayViewsAsync(),
                    PopularMovies = await GetPopularMoviesAsync(10),
                    RecentMovies = await GetRecentMoviesAsync(10)
                };

                _cache.Set(cacheKey, stats, _cacheTime);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return new StatisticsViewModel();
            }
        }

        public async Task<List<MovieListItemViewModel>> GetPopularMoviesAsync(int count = 10)
        {
            string cacheKey = $"popular_movies_{count}";

            if (_cache.TryGetValue(cacheKey, out List<MovieListItemViewModel> cachedMovies))
            {
                return cachedMovies;
            }

            try
            {
                var movies = await _context.CachedMovies
                    .OrderByDescending(m => m.ViewCount)
                    .Take(count)
                    .Select(m => new MovieListItemViewModel
                    {
                        Slug = m.Slug,
                        Name = m.Name,
                        OriginalName = m.OriginalName,
                        Year = m.Year,
                        ThumbUrl = m.ThumbUrl,
                        PosterUrl = m.PosterUrl,
                        Type = m.Type,
                        Quality = m.Quality,
                        ViewCount = m.ViewCount,
                        AverageRating = m.Statistic != null ? m.Statistic.AverageRating : 0,
                        IsFavorite = false,
                        WatchedPercentage = null
                    })
                    .ToListAsync();

                _cache.Set(cacheKey, movies, _cacheTime);
                return movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular movies");
                return new List<MovieListItemViewModel>();
            }
        }

        public async Task<List<MovieListItemViewModel>> GetRecentMoviesAsync(int count = 10)
        {
            string cacheKey = $"recent_movies_{count}";

            if (_cache.TryGetValue(cacheKey, out List<MovieListItemViewModel> cachedMovies))
            {
                return cachedMovies;
            }

            try
            {
                var movies = await _context.CachedMovies
                    .OrderByDescending(m => m.LastUpdated)
                    .Take(count)
                    .Select(m => new MovieListItemViewModel
                    {
                        Slug = m.Slug,
                        Name = m.Name,
                        OriginalName = m.OriginalName,
                        Year = m.Year,
                        ThumbUrl = m.ThumbUrl,
                        PosterUrl = m.PosterUrl,
                        Type = m.Type,
                        Quality = m.Quality,
                        ViewCount = m.ViewCount,
                        AverageRating = m.Statistic != null ? m.Statistic.AverageRating : 0,
                        IsFavorite = false,
                        WatchedPercentage = null
                    })
                    .ToListAsync();

                _cache.Set(cacheKey, movies, _cacheTime);
                return movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent movies");
                return new List<MovieListItemViewModel>();
            }
        }

        public async Task<int> GetTotalViewsAsync()
        {
            string cacheKey = "total_views";

            if (_cache.TryGetValue(cacheKey, out int cachedViews))
            {
                return cachedViews;
            }

            try
            {
                var totalViews = await _context.CachedMovies.SumAsync(m => m.ViewCount);
                _cache.Set(cacheKey, totalViews, _cacheTime);
                return totalViews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total views");
                return 0;
            }
        }

        public async Task<int> GetTodayViewsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var todayViews = await _context.MovieStatistics
                    .Where(s => s.LastUpdated >= today && s.LastUpdated < tomorrow)
                    .SumAsync(s => s.ViewCount);

                return todayViews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's views");
                return 0;
            }
        }
    }
}
