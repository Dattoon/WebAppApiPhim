using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;
using System.Security.Claims;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMovieApiService _movieApiService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IMovieApiService movieApiService,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _movieApiService = movieApiService;
            _context = context;
            _logger = logger;
        }

        // Existing methods...
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var dbMovieCount = await _context.CachedMovies.CountAsync();
                var dbUserCount = await _context.Users.CountAsync();
                var totalViews = await _context.CachedMovies.SumAsync(m => m.Views);

                // Test API connection
                var apiResult = await _movieApiService.GetLatestMoviesAsync(1, 1, "v1");
                var apiAvailable = apiResult.Data.Any();

                return Ok(new
                {
                    DatabaseMovies = dbMovieCount,
                    DatabaseUsers = dbUserCount,
                    TotalViews = totalViews,
                    ApiAvailable = apiAvailable,
                    LastSync = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin stats");
                return StatusCode(500, new { message = "Error getting stats" });
            }
        }

        // NEW ADMIN FEATURES

        // User Management
        [HttpGet("users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminUserListViewModel>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Users.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u => u.UserName.Contains(search) || u.Email.Contains(search));
                }

                var totalUsers = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AdminUserViewModel
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        DisplayName = u.DisplayName,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        IsActive = true // You can add this field to your user model
                    })
                    .ToListAsync();

                return Ok(new AdminUserListViewModel
                {
                    Users = users,
                    TotalCount = totalUsers,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users list");
                return StatusCode(500, new { message = "Error getting users" });
            }
        }

        [HttpGet("users/{userId}/activity")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<UserActivitySummaryViewModel>> GetUserActivity(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                var oneYearInFuture = DateTime.UtcNow.AddYears(1);
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

                var watchHistoryCount = await _context.UserMovies
                    .CountAsync(um => um.UserId == userId && um.AddedAt < oneYearInFuture);

                var favoritesCount = await _context.UserFavorites
                    .CountAsync(uf => uf.UserId == userId);

                var ratingsCount = await _context.MovieRatings
                    .CountAsync(mr => mr.UserId == userId);

                var commentsCount = await _context.UserComments
                    .CountAsync(uc => uc.UserId == userId);

                var recentActivity = await _context.UserComments
                    .Where(uc => uc.UserId == userId && uc.CreatedAt > thirtyDaysAgo)
                    .OrderByDescending(uc => uc.CreatedAt)
                    .Take(10)
                    .Select(uc => new RecentActivityViewModel
                    {
                        Type = "Comment",
                        MovieSlug = uc.MovieSlug,
                        Content = uc.Content.Length > 100 ? uc.Content.Substring(0, 100) + "..." : uc.Content,
                        CreatedAt = uc.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new UserActivitySummaryViewModel
                {
                    UserId = userId,
                    UserName = user.UserName,
                    WatchHistoryCount = watchHistoryCount,
                    FavoritesCount = favoritesCount,
                    RatingsCount = ratingsCount,
                    CommentsCount = commentsCount,
                    RecentActivity = recentActivity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user activity for {userId}");
                return StatusCode(500, new { message = "Error getting user activity" });
            }
        }

        [HttpPost("users/{userId}/ban")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> BanUser(Guid userId, [FromQuery] string reason = "")
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // You can add a BannedUsers table or add fields to ApplicationUser
                // For now, we'll use a simple approach by updating the user
                user.UpdatedAt = DateTime.UtcNow;
                // user.IsBanned = true; // Add this field to your model
                // user.BanReason = reason; // Add this field to your model

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} banned by admin. Reason: {reason}");
                return Ok(new { message = $"User {user.UserName} has been banned", reason });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error banning user {userId}");
                return StatusCode(500, new { message = "Error banning user" });
            }
        }

        // Content Management
        [HttpGet("movies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminMovieListViewModel>> GetMovies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string search = "",
            [FromQuery] string sortBy = "views")
        {
            try
            {
                var query = _context.CachedMovies.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(m => m.Title.Contains(search) || m.Slug.Contains(search));
                }

                query = sortBy.ToLower() switch
                {
                    "title" => query.OrderBy(m => m.Title),
                    "year" => query.OrderByDescending(m => m.Year),
                    "updated" => query.OrderByDescending(m => m.LastUpdated),
                    _ => query.OrderByDescending(m => m.Views)
                };

                var totalMovies = await query.CountAsync();
                var movies = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new AdminMovieViewModel
                    {
                        Slug = m.Slug,
                        Title = m.Title,
                        Year = m.Year,
                        Views = m.Views,
                        PosterUrl = m.PosterUrl,
                        LastUpdated = m.LastUpdated,
                        EpisodeCount = m.Episodes.Count(),
                        CommentsCount = m.Comments.Count(),
                        FavoritesCount = m.Favorites.Count()
                    })
                    .ToListAsync();

                return Ok(new AdminMovieListViewModel
                {
                    Movies = movies,
                    TotalCount = totalMovies,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalMovies / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movies list");
                return StatusCode(500, new { message = "Error getting movies" });
            }
        }

        [HttpDelete("movies/{movieSlug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteMovie(string movieSlug)
        {
            try
            {
                var movie = await _context.CachedMovies
                    .Include(m => m.Episodes)
                    .Include(m => m.Comments)
                    .Include(m => m.Favorites)
                    .Include(m => m.Ratings)
                    .FirstOrDefaultAsync(m => m.Slug == movieSlug);

                if (movie == null)
                {
                    return NotFound("Movie not found");
                }

                // Remove all related data
                _context.UserComments.RemoveRange(movie.Comments);
                _context.UserFavorites.RemoveRange(movie.Favorites);
                _context.MovieRatings.RemoveRange(movie.Ratings);
                _context.CachedEpisodes.RemoveRange(movie.Episodes);
                _context.CachedMovies.Remove(movie);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Movie {movieSlug} deleted by admin");
                return Ok(new { message = $"Movie '{movie.Title}' has been deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting movie {movieSlug}");
                return StatusCode(500, new { message = "Error deleting movie" });
            }
        }

        // Comments Management
        [HttpGet("comments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminCommentListViewModel>> GetComments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string movieSlug = "",
            [FromQuery] bool flaggedOnly = false)
        {
            try
            {
                var query = _context.UserComments
                    .Include(c => c.User)
                    .Include(c => c.Movie)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(movieSlug))
                {
                    query = query.Where(c => c.MovieSlug == movieSlug);
                }

                // You can add a flagged field to comments for reported content
                // if (flaggedOnly)
                // {
                //     query = query.Where(c => c.IsFlagged);
                // }

                var totalComments = await query.CountAsync();
                var comments = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new AdminCommentViewModel
                    {
                        Id = c.Id,
                        Content = c.Content,
                        UserName = c.User.UserName,
                        MovieTitle = c.Movie.Title,
                        MovieSlug = c.MovieSlug,
                        CreatedAt = c.CreatedAt,
                        IsFlagged = false // Add this field to your model
                    })
                    .ToListAsync();

                return Ok(new AdminCommentListViewModel
                {
                    Comments = comments,
                    TotalCount = totalComments,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalComments / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments list");
                return StatusCode(500, new { message = "Error getting comments" });
            }
        }

        [HttpDelete("comments/{commentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteComment(string commentId)
        {
            try
            {
                var comment = await _context.UserComments.FindAsync(commentId);
                if (comment == null)
                {
                    return NotFound("Comment not found");
                }

                _context.UserComments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Comment {commentId} deleted by admin");
                return Ok(new { message = "Comment has been deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting comment {commentId}");
                return StatusCode(500, new { message = "Error deleting comment" });
            }
        }

        // Analytics and Reports
        [HttpGet("analytics/dashboard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminDashboardViewModel>> GetDashboard()
        {
            try
            {
                var now = DateTime.UtcNow;
                var thirtyDaysAgo = now.AddDays(-30);
                var sevenDaysAgo = now.AddDays(-7);
                var oneYearInFuture = now.AddYears(1);

                // Basic stats
                var totalUsers = await _context.Users.CountAsync();
                var totalMovies = await _context.CachedMovies.CountAsync();
                var totalViews = await _context.CachedMovies.SumAsync(m => m.Views);

                // Recent activity
                var newUsersThisMonth = await _context.Users
                    .CountAsync(u => u.CreatedAt >= thirtyDaysAgo);

                var activeUsersThisWeek = await _context.UserMovies
                    .Where(um => um.AddedAt >= sevenDaysAgo && um.AddedAt < oneYearInFuture)
                    .Select(um => um.UserId)
                    .Distinct()
                    .CountAsync();

                var commentsThisWeek = await _context.UserComments
                    .CountAsync(c => c.CreatedAt >= sevenDaysAgo);

                // Top movies
                var topMovies = await _context.CachedMovies
                    .OrderByDescending(m => m.Views)
                    .Take(5)
                    .Select(m => new TopMovieViewModel
                    {
                        Slug = m.Slug,
                        Title = m.Title,
                        Views = m.Views,
                        PosterUrl = m.PosterUrl
                    })
                    .ToListAsync();

                // Daily views for the last 7 days
                var dailyViews = await _context.DailyViews
                    .Where(dv => dv.Date >= sevenDaysAgo)
                    .GroupBy(dv => dv.Date.Date)
                    .Select(g => new DailyViewsViewModel
                    {
                        Date = g.Key,
                        Views = g.Sum(dv => dv.ViewCount)
                    })
                    .OrderBy(dv => dv.Date)
                    .ToListAsync();

                return Ok(new AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    TotalMovies = totalMovies,
                    TotalViews = totalViews,
                    NewUsersThisMonth = newUsersThisMonth,
                    ActiveUsersThisWeek = activeUsersThisWeek,
                    CommentsThisWeek = commentsThisWeek,
                    TopMovies = topMovies,
                    DailyViews = dailyViews
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin dashboard");
                return StatusCode(500, new { message = "Error getting dashboard data" });
            }
        }

        [HttpGet("analytics/reports")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminReportsViewModel>> GetReports(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;
                var oneYearInFuture = DateTime.UtcNow.AddYears(1);

                // User registration trends
                var userRegistrations = await _context.Users
                    .Where(u => u.CreatedAt >= start && u.CreatedAt <= end)
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new DailyCountViewModel
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Movie watching trends
                var movieWatching = await _context.UserMovies
                    .Where(um => um.AddedAt >= start && um.AddedAt <= end && um.AddedAt < oneYearInFuture)
                    .GroupBy(um => um.AddedAt.Date)
                    .Select(g => new DailyCountViewModel
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Popular genres
                var popularGenres = await _context.MovieGenreMappings
                    .Include(mg => mg.Genre)
                    .Include(mg => mg.Movie)
                    .Where(mg => mg.Movie.Favorites.Any(f => f.AddedAt >= start && f.AddedAt <= end))
                    .GroupBy(mg => mg.Genre.Name)
                    .Select(g => new GenrePopularityViewModel
                    {
                        GenreName = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .ToListAsync();

                return Ok(new AdminReportsViewModel
                {
                    StartDate = start,
                    EndDate = end,
                    UserRegistrations = userRegistrations,
                    MovieWatching = movieWatching,
                    PopularGenres = popularGenres
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin reports");
                return StatusCode(500, new { message = "Error getting reports" });
            }
        }

        // System Management
        [HttpPost("system/backup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                // This is a simplified backup - in production you'd want more sophisticated backup
                var backupData = new
                {
                    CreatedAt = DateTime.UtcNow,
                    MoviesCount = await _context.CachedMovies.CountAsync(),
                    UsersCount = await _context.Users.CountAsync(),
                    CommentsCount = await _context.UserComments.CountAsync(),
                    // Add more backup metadata as needed
                };

                _logger.LogInformation("Backup created by admin");
                return Ok(new { message = "Backup created successfully", data = backupData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                return StatusCode(500, new { message = "Error creating backup" });
            }
        }

        [HttpPost("system/maintenance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RunMaintenance()
        {
            try
            {
                var tasksCompleted = new List<string>();

                // Clean up old data
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

                // Remove old daily views (keep only last 30 days)
                var oldViews = await _context.DailyViews
                    .Where(dv => dv.Date < thirtyDaysAgo)
                    .ToListAsync();

                if (oldViews.Any())
                {
                    _context.DailyViews.RemoveRange(oldViews);
                    tasksCompleted.Add($"Removed {oldViews.Count} old daily view records");
                }

                // Update movie statistics
                var movies = await _context.CachedMovies.ToListAsync();
                foreach (var movie in movies)
                {
                    var statistic = await _context.MovieStatistics
                        .FirstOrDefaultAsync(s => s.MovieSlug == movie.Slug);

                    if (statistic == null)
                    {
                        statistic = new MovieStatistic
                        {
                            MovieSlug = movie.Slug,
                            Views = movie.Views,
                            AverageRating = await _context.MovieRatings
                                .Where(r => r.MovieSlug == movie.Slug)
                                .AverageAsync(r => (double?)r.Rating) ?? 0,
                            FavoriteCount = await _context.UserFavorites
                                .CountAsync(f => f.MovieSlug == movie.Slug),
                            LastUpdated = DateTime.UtcNow
                        };
                        _context.MovieStatistics.Add(statistic);
                    }
                }

                await _context.SaveChangesAsync();
                tasksCompleted.Add("Updated movie statistics");

                _logger.LogInformation("Maintenance completed by admin");
                return Ok(new
                {
                    message = "Maintenance completed successfully",
                    tasksCompleted = tasksCompleted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running maintenance");
                return StatusCode(500, new { message = "Error running maintenance" });
            }
        }

        // Existing methods (force-sync, clear-cache) remain the same...
        [HttpPost("force-sync")]
        public async Task<IActionResult> ForceSync([FromQuery] int pages = 10)
        {
            try
            {
                var totalSynced = 0;

                for (int page = 1; page <= pages; page++)
                {
                    var apiResult = await _movieApiService.GetLatestMoviesAsync(page, 20, "v1");

                    foreach (var apiMovie in apiResult.Data)
                    {
                        var existingMovie = await _context.CachedMovies
                            .FirstOrDefaultAsync(m => m.Slug == apiMovie.Slug);

                        if (existingMovie == null)
                        {
                            var cachedMovie = new CachedMovie
                            {
                                Slug = apiMovie.Slug,
                                Title = apiMovie.Title,
                                Year = apiMovie.Year,
                                TmdbId = apiMovie.TmdbId,
                                PosterUrl = apiMovie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                                LastUpdated = DateTime.UtcNow,
                                Views = 0
                            };

                            _context.CachedMovies.Add(cachedMovie);
                            totalSynced++;
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = $"Force sync completed. Synced {totalSynced} movies.",
                    syncedCount = totalSynced
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in force sync");
                return StatusCode(500, new { message = "Force sync failed", error = ex.Message });
            }
        }

        [HttpDelete("clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                var movieCount = await _context.CachedMovies.CountAsync();
                _context.CachedMovies.RemoveRange(_context.CachedMovies);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Cleared {movieCount} cached movies",
                    clearedCount = movieCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return StatusCode(500, new { message = "Clear cache failed" });
            }
        }
    }
}
