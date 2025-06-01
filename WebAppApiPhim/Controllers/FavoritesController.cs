using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using System.Security.Claims;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(ApplicationDbContext context, ILogger<FavoritesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // POST: api/favorites
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserFavorite>> AddFavorite([FromQuery] string movieSlug)
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

                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == Guid.Parse(userId) && f.MovieSlug == movieSlug);

                if (existingFavorite != null)
                {
                    return BadRequest("Movie is already in your favorites.");
                }

                var favorite = new UserFavorite
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = Guid.Parse(userId),
                    MovieSlug = movieSlug,
                    AddedAt = DateTime.UtcNow
                };

                _context.UserFavorites.Add(favorite);

                // Update favorite count in MovieStatistic
                await UpdateMovieStatistics(movieSlug, incrementFavorites: true);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Added movie {movieSlug} to favorites for user {userId}");

                return CreatedAtAction(nameof(GetFavorites), new { }, favorite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding favorite for movie with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the favorite.");
            }
        }

        // GET: api/favorites
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<UserFavorite>>> GetFavorites()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var favorites = await _context.UserFavorites
                    .AsNoTracking()
                    .Include(f => f.Movie)
                    .Where(f => f.UserId == Guid.Parse(userId))
                    .OrderByDescending(f => f.AddedAt)
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {favorites.Count} favorites for user {userId}");
                return Ok(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorites");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving favorites.");
            }
        }

        // DELETE: api/favorites/{movieSlug}
        [HttpDelete("{movieSlug}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveFavorite(string movieSlug)
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

                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == Guid.Parse(userId) && f.MovieSlug == movieSlug);

                if (favorite == null)
                {
                    _logger.LogWarning($"Favorite not found for movie {movieSlug} by user {userId}.");
                    return NotFound($"Favorite not found for movie {movieSlug}.");
                }

                _context.UserFavorites.Remove(favorite);

                // Update favorite count in MovieStatistic
                await UpdateMovieStatistics(movieSlug, incrementFavorites: false);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Removed movie {movieSlug} from favorites for user {userId}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing favorite for movie with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing the favorite.");
            }
        }

        private async Task UpdateMovieStatistics(string movieSlug, bool incrementFavorites)
        {
            var statistic = await _context.MovieStatistics.FirstOrDefaultAsync(s => s.MovieSlug == movieSlug);
            if (statistic != null)
            {
                if (incrementFavorites)
                    statistic.FavoriteCount++;
                else
                    statistic.FavoriteCount = Math.Max(0, statistic.FavoriteCount - 1);

                statistic.LastUpdated = DateTime.UtcNow;
                _context.MovieStatistics.Update(statistic);
            }
        }

        private string? GetUserIdFromClaims()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
        }
    }
}
