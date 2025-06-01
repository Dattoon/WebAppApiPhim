using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<UserMovie>>> GetWatchHistory()
        {
            try
            {
                // Debug: Log authentication info
                _logger.LogInformation($"User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User.Identity.AuthenticationType: {User.Identity?.AuthenticationType}");
                _logger.LogInformation($"Claims count: {User.Claims.Count()}");

                // Log all claims for debugging
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
                }

                var userId = GetUserIdFromClaims();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized("User not authenticated.");
                }

                _logger.LogInformation($"Processing request for user: {userId}");

                var watchHistory = await _context.UserMovies
                    .AsNoTracking()
                    .Include(um => um.Movie)
                    .Where(um => um.UserId == Guid.Parse(userId))
                    .OrderByDescending(um => um.AddedAt)
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {watchHistory.Count} watch history items for user {userId}");
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserMovie>> AddToWatchHistory([FromQuery] string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var userId = GetUserIdFromClaims();
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
                    _logger.LogInformation($"Updated watch history for movie {movieSlug} by user {userId}");
                    return Ok(existingEntry);
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
                    _logger.LogInformation($"Added movie {movieSlug} to watch history for user {userId}");
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveFromWatchHistory(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var userId = GetUserIdFromClaims();
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
                _logger.LogInformation($"Removed movie {movieSlug} from watch history for user {userId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing movie from watch history with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing from watch history.");
            }
        }

        private string? GetUserIdFromClaims()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            _logger.LogInformation($"Extracted user ID: {userId}");
            return userId;
        }

        // Debug endpoint to test authentication
        [HttpGet("debug")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Debug()
        {
            var authInfo = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                AuthenticationType = User.Identity?.AuthenticationType,
                Name = User.Identity?.Name,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                UserId = GetUserIdFromClaims()
            };

            return Ok(authInfo);
        }
    }
}
