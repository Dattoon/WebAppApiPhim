using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WatchLaterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WatchLaterController> _logger;

        public WatchLaterController(ApplicationDbContext context, ILogger<WatchLaterController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/watchlater
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<UserMovie>>> GetWatchLaterQueue()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // We'll reuse the UserMovies table but with a different query pattern
                // Movies in the watch later queue will have a specific AddedAt date in the future
                // This is a clever way to reuse the existing table without modifying the schema
                var oneYearInFuture = DateTime.UtcNow.AddYears(1);
                var watchLaterQueue = await _context.UserMovies
                    .AsNoTracking()
                    .Include(um => um.Movie)
                    .Where(um => um.UserId == Guid.Parse(userId) && um.AddedAt > oneYearInFuture)
                    .OrderBy(um => um.AddedAt)
                    .ToListAsync();

                if (!watchLaterQueue.Any())
                {
                    _logger.LogWarning($"No watch later queue found for user {userId}.");
                    return NotFound($"No watch later queue found for user {userId}.");
                }

                return Ok(watchLaterQueue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving watch later queue");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving watch later queue.");
            }
        }

        // POST: api/watchlater
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserMovie>> AddToWatchLaterQueue([FromQuery] string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var movieExists = await _context.CachedMovies.AnyAsync(m => m.Slug == movieSlug);
                if (!movieExists)
                {
                    _logger.LogWarning($"Movie with slug {movieSlug} not found.");
                    return NotFound($"Movie with slug {movieSlug} not found.");
                }

                // Check if movie is already in watch later queue
                var oneYearInFuture = DateTime.UtcNow.AddYears(1);
                var existingEntry = await _context.UserMovies
                    .FirstOrDefaultAsync(um => um.UserId == Guid.Parse(userId) &&
                                              um.MovieSlug == movieSlug &&
                                              um.AddedAt > oneYearInFuture);

                if (existingEntry != null)
                {
                    return BadRequest("Movie is already in your watch later queue.");
                }

                // Add to watch later queue with a future date to distinguish from watch history
                var watchLaterDate = DateTime.UtcNow.AddYears(1).AddMinutes(new Random().Next(1, 1000));
                var newEntry = new UserMovie
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = Guid.Parse(userId),
                    MovieSlug = movieSlug,
                    AddedAt = watchLaterDate // Use future date to mark as watch later
                };

                _context.UserMovies.Add(newEntry);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetWatchLaterQueue), new { }, newEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding movie to watch later queue with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding to watch later queue.");
            }
        }

        // DELETE: api/watchlater/{movieSlug}
        [HttpDelete("{movieSlug}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveFromWatchLaterQueue(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Find entry in watch later queue
                var oneYearInFuture = DateTime.UtcNow.AddYears(1);
                var entry = await _context.UserMovies
                    .FirstOrDefaultAsync(um => um.UserId == Guid.Parse(userId) &&
                                              um.MovieSlug == movieSlug &&
                                              um.AddedAt > oneYearInFuture);

                if (entry == null)
                {
                    _logger.LogWarning($"Watch later entry not found for movie {movieSlug} by user {userId}.");
                    return NotFound($"Watch later entry not found for movie {movieSlug}.");
                }

                _context.UserMovies.Remove(entry);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing movie from watch later queue with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing from watch later queue.");
            }
        }
    }
}