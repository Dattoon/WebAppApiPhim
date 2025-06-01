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
    public class RatingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RatingsController> _logger;

        public RatingsController(ApplicationDbContext context, ILogger<RatingsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // POST: api/ratings
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MovieRating>> RateMovie(
            [FromQuery] string movieSlug,
            [FromQuery] double rating)
        {
            if (string.IsNullOrWhiteSpace(movieSlug) || rating < 0 || rating > 10)
            {
                _logger.LogWarning("Invalid movieSlug or rating parameters provided.");
                return BadRequest("MovieSlug is required and rating must be between 0 and 10.");
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

                var existingRating = await _context.MovieRatings
                    .FirstOrDefaultAsync(r => r.UserId == Guid.Parse(userId) && r.MovieSlug == movieSlug);

                MovieRating movieRating;

                if (existingRating == null)
                {
                    movieRating = new MovieRating
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = Guid.Parse(userId),
                        MovieSlug = movieSlug,
                        Rating = rating,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.MovieRatings.Add(movieRating);
                    _logger.LogInformation($"Created new rating {rating} for movie {movieSlug} by user {userId}");
                }
                else
                {
                    existingRating.Rating = rating;
                    existingRating.CreatedAt = DateTime.UtcNow;
                    _context.MovieRatings.Update(existingRating);
                    movieRating = existingRating;
                    _logger.LogInformation($"Updated rating to {rating} for movie {movieSlug} by user {userId}");
                }

                // Update average rating in MovieStatistic
                await UpdateMovieAverageRating(movieSlug);

                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetRating), new { movieSlug }, movieRating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rating movie with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while rating the movie.");
            }
        }

        // GET: api/ratings/{movieSlug}
        [HttpGet("{movieSlug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MovieRating>> GetRating(string movieSlug)
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

                var rating = await _context.MovieRatings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.UserId == Guid.Parse(userId) && r.MovieSlug == movieSlug);

                if (rating == null)
                {
                    _logger.LogInformation($"No rating found for movie {movieSlug} by user {userId}.");
                    return NotFound($"No rating found for movie {movieSlug}.");
                }

                return Ok(rating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving rating for movie {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the rating.");
            }
        }

        // GET: api/ratings/{movieSlug}/average
        [HttpGet("{movieSlug}/average")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetAverageRating(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var ratings = await _context.MovieRatings
                    .Where(r => r.MovieSlug == movieSlug)
                    .ToListAsync();

                if (!ratings.Any())
                {
                    return Ok(new { averageRating = 0.0, totalRatings = 0 });
                }

                var averageRating = ratings.Average(r => r.Rating);
                var totalRatings = ratings.Count;

                return Ok(new { averageRating = Math.Round(averageRating, 1), totalRatings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving average rating for movie {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the average rating.");
            }
        }

        private async Task UpdateMovieAverageRating(string movieSlug)
        {
            var avgRating = await _context.MovieRatings
                .Where(r => r.MovieSlug == movieSlug)
                .AverageAsync(r => r.Rating);

            var statistic = await _context.MovieStatistics.FirstOrDefaultAsync(s => s.MovieSlug == movieSlug);
            if (statistic != null)
            {
                statistic.AverageRating = avgRating;
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
