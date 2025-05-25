using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using System.Threading.Tasks;
using System;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        public async Task<ActionResult<UserFavorite>> AddFavorite([FromQuery] string movieSlug)
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

                // Cập nhật số lượng yêu thích trong MovieStatistic
                var statistic = await _context.MovieStatistics.FirstOrDefaultAsync(s => s.MovieSlug == movieSlug);
                if (statistic != null)
                {
                    statistic.FavoriteCount++;
                    statistic.LastUpdated = DateTime.UtcNow;
                    _context.MovieStatistics.Update(statistic);
                }

                await _context.SaveChangesAsync();
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<UserFavorite>>> GetFavorites()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

                if (!favorites.Any())
                {
                    _logger.LogWarning($"No favorites found for user {Guid.Parse(userId)}.");
                    return NotFound($"No favorites found for user {Guid.Parse(userId)}.");
                }

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
        public async Task<IActionResult> RemoveFavorite(string movieSlug)
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

                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == Guid.Parse(userId) && f.MovieSlug == movieSlug);

                if (favorite == null)
                {
                    _logger.LogWarning($"Favorite not found for movie {movieSlug} by user {Guid.Parse(userId)}.");
                    return NotFound($"Favorite not found for movie {movieSlug}.");
                }

                _context.UserFavorites.Remove(favorite);

                // Cập nhật số lượng yêu thích trong MovieStatistic
                var statistic = await _context.MovieStatistics.FirstOrDefaultAsync(s => s.MovieSlug == movieSlug);
                if (statistic != null)
                {
                    statistic.FavoriteCount--;
                    statistic.LastUpdated = DateTime.UtcNow;
                    _context.MovieStatistics.Update(statistic);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing favorite for movie with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing the favorite.");
            }
        }
    }
}