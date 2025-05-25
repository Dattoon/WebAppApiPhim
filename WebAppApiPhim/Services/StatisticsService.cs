using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(
            ApplicationDbContext dbContext,
            ILogger<StatisticsService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> GetTotalMoviesAsync()
        {
            try
            {
                var count = await _dbContext.CachedMovies.CountAsync();
                _logger.LogInformation("Total movies retrieved: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total movies");
                return 0;
            }
        }

        public async Task<int> GetTotalUsersAsync()
        {
            try
            {
                var count = await _dbContext.Users.CountAsync(); // Use AspNetUsers table
                _logger.LogInformation("Total users retrieved: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total users");
                return 0;
            }
        }

        public async Task<long> GetMovieViewCountAsync(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("GetMovieViewCountAsync failed: Movie slug is empty");
                return 0;
            }

            try
            {
                var movie = await _dbContext.CachedMovies
                    .FirstOrDefaultAsync(m => m.Slug == movieSlug);

                if (movie == null)
                {
                    _logger.LogWarning("Movie not found for slug: {MovieSlug}", movieSlug);
                    return 0;
                }

                _logger.LogInformation("View count for movie {MovieSlug}: {ViewCount}", movieSlug, movie.Views);
                return movie.Views;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving view count for movie: {MovieSlug}", movieSlug);
                return 0;
            }
        }

        public async Task<(string Quality, int Count)[]> GetMoviesByQualityAsync()
        {
            try
            {
                var moviesByQuality = await _dbContext.CachedMovies
                    .GroupBy(m => m.Resolution)
                    .Select(g => new { Quality = g.Key, Count = g.Count() })
                    .ToArrayAsync();

                var result = moviesByQuality
                    .Select(x => (x.Quality ?? "Unknown", x.Count))
                    .ToArray();

                _logger.LogInformation("Movies by quality retrieved: {Count} groups", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movies by quality");
                return [];
            }
        }

        public void InvalidateCache(string slug)
        {
            throw new NotImplementedException();
        }
    }
}