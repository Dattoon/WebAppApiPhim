using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    /// <summary>
    /// Service sử dụng các bảng USER hiện có mà không cần thay đổi database
    /// </summary>
    public interface IExistingUserService
    {
        Task RecordMovieViewAsync(Guid userId, string movieSlug);
        Task AddToFavoritesAsync(Guid userId, string movieSlug);
        Task RemoveFromFavoritesAsync(Guid userId, string movieSlug);
        Task AddToWatchlistAsync(Guid userId, string movieSlug);
        Task RemoveFromWatchlistAsync(Guid userId, string movieSlug);
        Task RateMovieAsync(Guid userId, string movieSlug, double rating);
        Task<List<CachedMovie>> GetUserFavoritesAsync(Guid userId, int page = 1, int limit = 20);
        Task<List<CachedMovie>> GetUserWatchlistAsync(Guid userId, int page = 1, int limit = 20);
        Task UpdateEpisodeProgressAsync(Guid userId, string episodeId, double watchedPercentage);
    }

    public class ExistingUserService : IExistingUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExistingUserService> _logger;

        public ExistingUserService(ApplicationDbContext context, ILogger<ExistingUserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RecordMovieViewAsync(Guid userId, string movieSlug)
        {
            try
            {
                // Tăng view count cho movie
                var movie = await _context.CachedMovies.FindAsync(movieSlug);
                if (movie != null)
                {
                    movie.Views++;
                    await _context.SaveChangesAsync();
                }

                // Cập nhật DailyView
                var today = DateTime.UtcNow.Date;
                var dailyView = await _context.DailyViews
                    .FirstOrDefaultAsync(dv => dv.MovieSlug == movieSlug && dv.Date.Date == today);

                if (dailyView == null)
                {
                    dailyView = new DailyView
                    {
                        Id = Guid.NewGuid().ToString(),
                        MovieSlug = movieSlug,
                        Date = today,
                        ViewCount = 1,
                        Views = 1
                    };
                    _context.DailyViews.Add(dailyView);
                }
                else
                {
                    dailyView.ViewCount++;
                    dailyView.Views++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording view for user {userId}, movie {movieSlug}", userId, movieSlug);
            }
        }

        public async Task AddToFavoritesAsync(Guid userId, string movieSlug)
        {
            var existing = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);

            if (existing == null)
            {
                _context.UserFavorites.Add(new UserFavorite
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    MovieSlug = movieSlug,
                    AddedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromFavoritesAsync(Guid userId, string movieSlug)
        {
            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);

            if (favorite != null)
            {
                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddToWatchlistAsync(Guid userId, string movieSlug)
        {
            var existing = await _context.UserMovies
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieSlug == movieSlug);

            if (existing == null)
            {
                _context.UserMovies.Add(new UserMovie
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    MovieSlug = movieSlug,
                    AddedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromWatchlistAsync(Guid userId, string movieSlug)
        {
            var watchlistItem = await _context.UserMovies
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieSlug == movieSlug);

            if (watchlistItem != null)
            {
                _context.UserMovies.Remove(watchlistItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RateMovieAsync(Guid userId, string movieSlug, double rating)
        {
            var existingRating = await _context.MovieRatings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieSlug == movieSlug);

            if (existingRating == null)
            {
                _context.MovieRatings.Add(new MovieRating
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    MovieSlug = movieSlug,
                    Rating = rating,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingRating.Rating = rating;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<CachedMovie>> GetUserFavoritesAsync(Guid userId, int page = 1, int limit = 20)
        {
            return await _context.UserFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Movie)
                .OrderByDescending(f => f.AddedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(f => f.Movie)
                .ToListAsync();
        }

        public async Task<List<CachedMovie>> GetUserWatchlistAsync(Guid userId, int page = 1, int limit = 20)
        {
            return await _context.UserMovies
                .Where(w => w.UserId == userId)
                .Include(w => w.Movie)
                .OrderByDescending(w => w.AddedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(w => w.Movie)
                .ToListAsync();
        }

        public async Task UpdateEpisodeProgressAsync(Guid userId, string episodeId, double watchedPercentage)
        {
            var progress = await _context.EpisodeProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.EpisodeId == episodeId);

            if (progress == null)
            {
                progress = new EpisodeProgress
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    EpisodeId = episodeId,
                    WatchedPercentage = watchedPercentage,
                    LastWatched = DateTime.UtcNow
                };
                _context.EpisodeProgresses.Add(progress);
            }
            else
            {
                progress.WatchedPercentage = Math.Max(progress.WatchedPercentage, watchedPercentage);
                progress.LastWatched = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
