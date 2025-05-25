using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebAppApiPhim.Services.Interfaces;
using WebAppApiPhim.Data;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Models;

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
