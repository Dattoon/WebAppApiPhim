using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedMoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<EnhancedMoviesController> _logger;

        public EnhancedMoviesController(
            ApplicationDbContext context,
            IMovieApiService movieApiService,
            ILogger<EnhancedMoviesController> logger)
        {
            _context = context;
            _movieApiService = movieApiService;
            _logger = logger;
        }

        /// <summary>
        /// Get movies with complete metadata
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MovieListResponse>> GetMoviesWithMetadata(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? genre = null,
            [FromQuery] string? country = null,
            [FromQuery] string? year = null)
        {
            try
            {
                var query = _context.CachedMovies
                    .Include(m => m.MovieGenreMappings)
                        .ThenInclude(mgm => mgm.Genre)
                    .Include(m => m.MovieCountryMappings)
                        .ThenInclude(mcm => mcm.Country)
                    .Include(m => m.MovieActors)
                        .ThenInclude(ma => ma.Actor)
                    .AsNoTracking()
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(genre))
                {
                    query = query.Where(m => m.MovieGenreMappings
                        .Any(mgm => mgm.Genre.Name.Contains(genre)));
                }

                if (!string.IsNullOrEmpty(country))
                {
                    query = query.Where(m => m.MovieCountryMappings
                        .Any(mcm => mcm.Country.Name.Contains(country)));
                }

                if (!string.IsNullOrEmpty(year))
                {
                    query = query.Where(m => m.Year == year);
                }

                var totalItems = await query.CountAsync();
                var movies = await query
                    .OrderByDescending(m => m.LastUpdated)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var response = new MovieListResponse
                {
                    Data = movies.Select(m => new MovieItem
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movies with metadata");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get movie detail with complete metadata and episodes
        /// </summary>
        [HttpGet("{slug}/complete")]
        public async Task<ActionResult> GetCompleteMovieDetail(string slug)
        {
            try
            {
                var movie = await _context.CachedMovies
                    .Include(m => m.Episodes)
                        .ThenInclude(e => e.EpisodeServers)
                    .Include(m => m.MovieGenreMappings)
                        .ThenInclude(mgm => mgm.Genre)
                    .Include(m => m.MovieCountryMappings)
                        .ThenInclude(mcm => mcm.Country)
                    .Include(m => m.MovieActors)
                        .ThenInclude(ma => ma.Actor)
                    .Include(m => m.Statistic)
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (movie == null)
                {
                    return NotFound(new { message = $"Movie with slug '{slug}' not found." });
                }

                // Build complete response
                var response = new
                {
                    movie.Slug,
                    movie.Title,
                    movie.Description,
                    movie.PosterUrl,
                    movie.ThumbUrl,
                    movie.Year,
                    movie.Director,
                    movie.Duration,
                    movie.Language,
                    movie.TmdbId,
                    movie.Rating,
                    movie.TrailerUrl,
                    movie.Views,
                    Genres = movie.MovieGenreMappings.Select(mgm => mgm.Genre.Name).ToList(),
                    Countries = movie.MovieCountryMappings.Select(mcm => mcm.Country.Name).ToList(),
                    Actors = movie.MovieActors.Select(ma => ma.Actor.Name).ToList(),
                    Episodes = movie.Episodes.Select(e => new
                    {
                        e.Id,
                        e.EpisodeNumber,
                        e.Title,
                        e.Url,
                        Servers = e.EpisodeServers.Select(s => new
                        {
                            s.ServerName,
                            s.ServerUrl
                        }).ToList()
                    }).OrderBy(e => e.EpisodeNumber).ToList(),
                    Statistics = movie.Statistic != null ? new
                    {
                        movie.Statistic.Views,
                        movie.Statistic.AverageRating,
                        movie.Statistic.FavoriteCount
                    } : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving complete movie detail: {slug}", slug);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Force sync specific movie with all metadata
        /// </summary>
        [HttpPost("{slug}/force-sync")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ForceSyncMovie(string slug)
        {
            try
            {
                // Get movie detail from API
                var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug, "v1");
                if (movieDetail == null)
                {
                    return NotFound(new { message = "Movie not found in external API" });
                }

                // Get images
                var images = await _movieApiService.GetMovieImagesAsync(slug);

                // Update or create movie
                var existingMovie = await _context.CachedMovies
                    .Include(m => m.Episodes)
                    .Include(m => m.MovieGenreMappings)
                    .Include(m => m.MovieCountryMappings)
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (existingMovie != null)
                {
                    // Update existing
                    existingMovie.Title = movieDetail.Title;
                    existingMovie.Description = movieDetail.Description ?? "";
                    existingMovie.Director = movieDetail.Director ?? "";
                    existingMovie.Duration = movieDetail.Duration ?? "";
                    existingMovie.Language = movieDetail.Language ?? "";
                    existingMovie.Year = movieDetail.Year ?? "";
                    existingMovie.TmdbId = movieDetail.TmdbId ?? "";
                    existingMovie.Rating = movieDetail.Rating;
                    existingMovie.TrailerUrl = movieDetail.TrailerUrl;
                    existingMovie.LastUpdated = DateTime.UtcNow;
                    existingMovie.RawData = System.Text.Json.JsonSerializer.Serialize(movieDetail);

                    if (images.Success)
                    {
                        existingMovie.PosterUrl = images.SubPoster;
                        existingMovie.ThumbUrl = images.SubThumb;
                    }

                    _context.CachedMovies.Update(existingMovie);
                }
                else
                {
                    // Create new
                    var newMovie = new CachedMovie
                    {
                        Slug = movieDetail.Slug,
                        Title = movieDetail.Title,
                        Description = movieDetail.Description ?? "",
                        PosterUrl = images.Success ? images.SubPoster : movieDetail.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                        ThumbUrl = images.Success ? images.SubThumb : movieDetail.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                        Year = movieDetail.Year ?? "",
                        Director = movieDetail.Director ?? "",
                        Duration = movieDetail.Duration ?? "",
                        Language = movieDetail.Language ?? "",
                        TmdbId = movieDetail.TmdbId ?? "",
                        Rating = movieDetail.Rating,
                        TrailerUrl = movieDetail.TrailerUrl,
                        Views = movieDetail.Views,
                        LastUpdated = DateTime.UtcNow,
                        RawData = System.Text.Json.JsonSerializer.Serialize(movieDetail)
                    };

                    _context.CachedMovies.Add(newMovie);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Movie synced successfully",
                    slug = slug,
                    title = movieDetail.Title,
                    episodeCount = movieDetail.Episodes?.Sum(e => e.Items.Count) ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force syncing movie: {slug}", slug);
                return StatusCode(500, new { message = "Force sync failed", error = ex.Message });
            }
        }



    }


}
