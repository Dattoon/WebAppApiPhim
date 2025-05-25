using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(
            ApplicationDbContext context,
            IMovieApiService movieApiService,
            ILogger<MoviesController> logger)
        {
            _context = context;
            _movieApiService = movieApiService;
            _logger = logger;
        }

        /// <summary>
        /// Get movies with real API integration
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MovieListResponse>> GetMovies(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string version = "v2")
        {
            try
            {
                // Try database first
                var dbMovies = await _context.CachedMovies
                    .AsNoTracking()
                    .OrderByDescending(m => m.LastUpdated)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var totalItems = await _context.CachedMovies.CountAsync();

                // If database has movies, return them
                if (dbMovies.Any())
                {
                    var response = new MovieListResponse
                    {
                        Data = dbMovies.Select(m => new MovieItem
                        {
                            Slug = m.Slug,
                            Title = m.Title,
                            PosterUrl = m.PosterUrl,
                            Year = m.Year,
                            TmdbId = m.TmdbId,
                            Modified = new ModifiedData { Time = m.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") }
                        }).ToList(),
                        Pagination = new Pagination
                        {
                            CurrentPage = page,
                            TotalPages = (int)Math.Ceiling((double)totalItems / limit),
                            TotalItems = totalItems,
                            Limit = limit
                        }
                    };

                    return Ok(response);
                }

                // If database is empty, fetch from real API
                _logger.LogInformation("Database empty, fetching from real API...");
                var apiResult = await _movieApiService.GetLatestMoviesAsync(page, limit, version);

                // Try to cache API results safely
                try
                {
                    await SafeCacheRealApiMoviesToDatabase(apiResult.Data);
                    _logger.LogInformation("Successfully cached {count} movies", apiResult.Data.Count);
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, "Failed to cache movies to database, returning API data anyway");
                }

                return Ok(apiResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movies");
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get movie detail with real API
        /// </summary>
        [HttpGet("{slug}")]
        public async Task<ActionResult> GetMovie(string slug, [FromQuery] string version = "v1")
        {
            try
            {
                // Try database first
                var movie = await _context.CachedMovies
                    .Include(m => m.Episodes)
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (movie != null)
                {
                    return Ok(movie);
                }

                // If not in database, try real API
                var apiMovie = await _movieApiService.GetMovieDetailBySlugAsync(slug, version);
                if (apiMovie != null)
                {
                    return Ok(apiMovie);
                }

                return NotFound(new { message = $"Movie with slug '{slug}' not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movie: {slug}", slug);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get real movie images
        /// </summary>
        [HttpGet("{slug}/images")]
        public async Task<ActionResult> GetMovieImages(string slug)
        {
            try
            {
                var images = await _movieApiService.GetMovieImagesAsync(slug);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie images: {slug}", slug);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search movies by filters using real API
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult> SearchMovies(
            [FromQuery] string name = null,
            [FromQuery] string genre = null,
            [FromQuery] string country = null,
            [FromQuery] string type = null,
            [FromQuery] string year = null,
            [FromQuery] int page = 1)
        {
            try
            {
                MovieListResponse result = null;

                if (!string.IsNullOrEmpty(name))
                {
                    result = await _movieApiService.GetMoviesByCategoryAsync($"name={Uri.EscapeDataString(name)}", page);
                }
                else if (!string.IsNullOrEmpty(genre))
                {
                    result = await _movieApiService.GetMoviesByGenreAsync(genre, page);
                }
                else if (!string.IsNullOrEmpty(country))
                {
                    result = await _movieApiService.GetMoviesByCountryAsync(country, page);
                }
                else if (!string.IsNullOrEmpty(type))
                {
                    result = await _movieApiService.GetMoviesByTypeAsync(type, page);
                }
                else if (!string.IsNullOrEmpty(year))
                {
                    result = await _movieApiService.GetMoviesByCategoryAsync($"year={year}", page);
                }
                else
                {
                    result = await _movieApiService.GetLatestMoviesAsync(page, 20, "v2");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching movies");
                return StatusCode(500, new { message = "Search failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Sync movies from real API
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncMovies([FromQuery] int pages = 3, [FromQuery] string version = "v2")
        {
            try
            {
                var syncedCount = 0;
                var errors = new List<string>();

                for (int page = 1; page <= pages; page++)
                {
                    try
                    {
                        var apiResult = await _movieApiService.GetLatestMoviesAsync(page, 20, version);
                        var cached = await SafeCacheRealApiMoviesToDatabase(apiResult.Data);
                        syncedCount += cached;

                        _logger.LogInformation("Synced page {page}, cached {count} movies", page, cached);
                    }
                    catch (Exception pageEx)
                    {
                        var error = $"Page {page}: {pageEx.Message}";
                        errors.Add(error);
                        _logger.LogError(pageEx, "Error syncing page {page}", page);
                    }
                }

                return Ok(new
                {
                    message = $"Sync completed. Successfully synced {syncedCount} movies",
                    syncedCount = syncedCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing movies");
                return StatusCode(500, new { message = "Sync failed", error = ex.Message });
            }
        }

        [HttpGet("test-real-api")]
        public async Task<IActionResult> TestRealApi([FromQuery] string version = "v2")
        {
            try
            {
                var result = await _movieApiService.GetLatestMoviesAsync(1, 5, version);
                return Ok(new
                {
                    success = true,
                    version = version,
                    moviesCount = result.Data.Count,
                    movies = result.Data.Take(3).Select(m => new {
                        m.Slug,
                        m.Title,
                        m.Year,
                        m.TmdbId,
                        m.PosterUrl,
                        Modified = m.Modified?.Time
                    }),
                    pagination = result.Pagination
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // Enhanced caching method for real API data
        private async Task<int> SafeCacheRealApiMoviesToDatabase(List<MovieItem> apiMovies)
        {
            var cachedCount = 0;

            foreach (var apiMovie in apiMovies)
            {
                try
                {
                    // Skip if essential data is missing
                    if (string.IsNullOrWhiteSpace(apiMovie.Slug) || string.IsNullOrWhiteSpace(apiMovie.Title))
                    {
                        _logger.LogWarning("Skipping movie with missing slug or title");
                        continue;
                    }

                    var existingMovie = await _context.CachedMovies
                        .FirstOrDefaultAsync(m => m.Slug == apiMovie.Slug);

                    if (existingMovie == null)
                    {
                        // Get real poster URL from image API
                        var posterUrl = apiMovie.PosterUrl;
                        if (string.IsNullOrEmpty(posterUrl))
                        {
                            try
                            {
                                var imageResponse = await _movieApiService.GetMovieImagesAsync(apiMovie.Slug);
                                if (imageResponse.Success && !string.IsNullOrEmpty(imageResponse.SubPoster))
                                {
                                    posterUrl = imageResponse.SubPoster;
                                }
                            }
                            catch
                            {
                                posterUrl = "/placeholder.svg?height=450&width=300";
                            }
                        }

                        var cachedMovie = new CachedMovie
                        {
                            Slug = apiMovie.Slug,
                            Title = apiMovie.Title,
                            PosterUrl = posterUrl ?? "/placeholder.svg?height=450&width=300",
                            LastUpdated = DateTime.UtcNow,
                            Views = 0,

                            // Optional fields with safe defaults
                            Description = "",
                            Director = "",
                            Duration = "",
                            Language = "",
                            ThumbUrl = posterUrl ?? "/placeholder.svg?height=450&width=300",
                            Year = apiMovie.Year ?? "",
                            TmdbId = apiMovie.TmdbId ?? "",
                            Resolution = "",
                            TrailerUrl = "",
                            Rating = null,
                            RawData = ""
                        };

                        _context.CachedMovies.Add(cachedMovie);
                        cachedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache movie: {slug}", apiMovie.Slug ?? "unknown");
                }
            }

            if (cachedCount > 0)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully saved {count} movies to database", cachedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save movies to database");
                    throw;
                }
            }

            return cachedCount;
        }
    }
}
