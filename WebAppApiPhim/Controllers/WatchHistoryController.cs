using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using StackExchange.Redis;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WatchHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WatchHistoryController> _logger;

        public WatchHistoryController(ApplicationDbContext context, ILogger<WatchHistoryController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/watchhistory
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<UserMovie>>> GetWatchHistory()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var watchHistory = await _context.UserMovies
                    .AsNoTracking()
                    .Include(um => um.Movie)
                    .Where(um => um.UserId == Guid.Parse(userId))
                    .OrderByDescending(um => um.AddedAt)
                    .ToListAsync();

                if (!watchHistory.Any())
                {
                    _logger.LogWarning($"No watch history found for user {userId}.");
                    return NotFound($"No watch history found for user {userId}.");
                }

                return Ok(watchHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving watch history");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving watch history.");
            }
        }

        // POST: api/watchhistory
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserMovie>> AddToWatchHistory([FromQuery] string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

                var existingEntry = await _context.UserMovies
                    .FirstOrDefaultAsync(um => um.UserId == Guid.Parse(userId) && um.MovieSlug == movieSlug);

                if (existingEntry != null)
                {
                    existingEntry.AddedAt = DateTime.UtcNow;
                    _context.UserMovies.Update(existingEntry);
                    await _context.SaveChangesAsync();
                    return CreatedAtAction(nameof(GetWatchHistory), new { }, existingEntry);
                }
                else
                {
                    var newEntry = new UserMovie
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = Guid.Parse(userId),
                        MovieSlug = movieSlug,
                        AddedAt = DateTime.UtcNow
                    };
                    _context.UserMovies.Add(newEntry);
                    await _context.SaveChangesAsync();
                    return CreatedAtAction(nameof(GetWatchHistory), new { }, newEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding movie to watch history with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding to watch history.");
            }
        }
        // DELETE: api/watchhistory/{movieSlug}
        [HttpDelete("{movieSlug}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveFromWatchHistory(string movieSlug)
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

                var entry = await _context.UserMovies
                    .FirstOrDefaultAsync(um => um.UserId == Guid.Parse(userId) && um.MovieSlug == movieSlug);

                if (entry == null)
                {
                    _logger.LogWarning($"Watch history entry not found for movie {movieSlug} by user {userId}.");
                    return NotFound($"Watch history entry not found for movie {movieSlug}.");
                }

                _context.UserMovies.Remove(entry);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing movie from watch history with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing from watch history.");
            }
        }
    }
}