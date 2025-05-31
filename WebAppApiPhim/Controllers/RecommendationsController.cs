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
    public class RecommendationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(ApplicationDbContext context, ILogger<RecommendationsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/recommendations
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CachedMovie>>> GetRecommendations()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Get user's watch history
                var watchHistory = await _context.UserMovies
                    .AsNoTracking()
                    .Where(um => um.UserId == Guid.Parse(userId))
                    .Select(um => um.MovieSlug)
                    .ToListAsync();

                if (!watchHistory.Any())
                {
                    // If no watch history, return trending movies
                    return await GetTrendingMovies();
                }

                // Get user's favorite genres based on watch history
                var favoriteGenres = await _context.MovieGenreMappings
                    .AsNoTracking()
                    .Where(mg => watchHistory.Contains(mg.MovieSlug))
                    .GroupBy(mg => mg.GenreId)
                    .Select(g => new { GenreId = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(3)
                    .Select(g => g.GenreId)
                    .ToListAsync();

                // Get recommendations based on favorite genres
                var recommendations = await _context.MovieGenreMappings
                    .AsNoTracking()
                    .Where(mg => favoriteGenres.Contains(mg.GenreId) && !watchHistory.Contains(mg.MovieSlug))
                    .Select(mg => mg.MovieSlug)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                // Get movie details
                var recommendedMovies = await _context.CachedMovies
                    .AsNoTracking()
                    .Where(m => recommendations.Contains(m.Slug))
                    .ToListAsync();

                if (!recommendedMovies.Any())
                {
                    return await GetTrendingMovies();
                }

                return Ok(recommendedMovies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommendations");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving recommendations.");
            }
        }

        // GET: api/recommendations/trending
        [HttpGet("trending")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CachedMovie>>> GetTrendingMovies()
        {
            try
            {
                // Get movies with highest view counts in the last 7 days
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

                var trendingMovieSlugs = await _context.DailyViews
                    .AsNoTracking()
                    .Where(dv => dv.Date >= sevenDaysAgo)
                    .GroupBy(dv => dv.MovieSlug)
                    .Select(g => new { MovieSlug = g.Key, TotalViews = g.Sum(dv => dv.ViewCount) })
                    .OrderByDescending(x => x.TotalViews)
                    .Take(10)
                    .Select(x => x.MovieSlug)
                    .ToListAsync();

                var trendingMovies = await _context.CachedMovies
                    .AsNoTracking()
                    .Where(m => trendingMovieSlugs.Contains(m.Slug))
                    .ToListAsync();

                if (!trendingMovies.Any())
                {
                    // Fallback to most viewed movies overall
                    trendingMovies = await _context.CachedMovies
                        .AsNoTracking()
                        .OrderByDescending(m => m.Views)
                        .Take(10)
                        .ToListAsync();
                }

                return Ok(trendingMovies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trending movies");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving trending movies.");
            }
        }

        // GET: api/recommendations/similar/{movieSlug}
        [HttpGet("similar/{movieSlug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CachedMovie>>> GetSimilarMovies(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                // Get genres of the specified movie
                var movieGenres = await _context.MovieGenreMappings
                    .AsNoTracking()
                    .Where(mg => mg.MovieSlug == movieSlug)
                    .Select(mg => mg.GenreId)
                    .ToListAsync();

                if (!movieGenres.Any())
                {
                    return NotFound($"Movie with slug {movieSlug} not found or has no genres.");
                }

                // Find movies with similar genres
                var similarMovieSlugs = await _context.MovieGenreMappings
                    .AsNoTracking()
                    .Where(mg => movieGenres.Contains(mg.GenreId) && mg.MovieSlug != movieSlug)
                    .GroupBy(mg => mg.MovieSlug)
                    .Select(g => new { MovieSlug = g.Key, MatchCount = g.Count() })
                    .OrderByDescending(x => x.MatchCount)
                    .Take(10)
                    .Select(x => x.MovieSlug)
                    .ToListAsync();

                var similarMovies = await _context.CachedMovies
                    .AsNoTracking()
                    .Where(m => similarMovieSlugs.Contains(m.Slug))
                    .ToListAsync();

                return Ok(similarMovies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving similar movies for {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving similar movies.");
            }
        }
    }
}