using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;
using System.Threading.Tasks;
using System;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStreamingService _streamingService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(ApplicationDbContext context, IStreamingService streamingService, ILogger<MoviesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _streamingService = streamingService ?? throw new ArgumentNullException(nameof(streamingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/movies/{slug}
        [HttpGet("{slug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CachedMovie>> GetMovie(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Invalid slug parameter provided.");
                return BadRequest("Slug is required.");
            }

            try
            {
                var movie = await _streamingService.GetCachedMovieAsync(slug);
                if (movie == null)
                {
                    _logger.LogWarning($"Movie with slug {slug} not found.");
                    return NotFound($"Movie with slug {slug} not found.");
                }

                return Ok(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving movie with slug {slug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the movie.");
            }
        }

        // GET: api/movies
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<MovieListResponse>> GetMovies(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            if (page < 1 || limit < 1)
            {
                _logger.LogWarning("Invalid page or limit parameters provided.");
                return BadRequest("Page and limit must be positive integers.");
            }

            try
            {
                var movies = await _context.CachedMovies
                    .AsNoTracking()
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var totalItems = await _context.CachedMovies.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / limit);

                var response = new MovieListResponse
                {
                    Data = movies.Select(m => new MovieItem
                    {
                        Slug = m.Slug,
                        Title = m.Title,
                        PosterUrl = m.PosterUrl,
                        Year = m.Year,
                        Modified = new ModifiedData { Time = m.LastUpdated.ToString("yyyy-MM-dd") }
                    }).ToList(),
                    Pagination = new Pagination
                    {
                        CurrentPage = page,
                        TotalPages = totalPages,
                        TotalItems = totalItems,
                        Limit = limit
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movie list");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the movie list.");
            }
        }

        // GET: api/movies/{slug}/episodes
        [HttpGet("{slug}/episodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<ServerViewModel>>> GetEpisodes(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Invalid slug parameter provided.");
                return BadRequest("Slug is required.");
            }

            try
            {
                var episodes = await _streamingService.GetEpisodesAsync(slug);
                if (episodes == null || !episodes.Any())
                {
                    _logger.LogWarning($"No episodes found for movie with slug {slug}.");
                    return NotFound($"No episodes found for movie with slug {slug}.");
                }

                return Ok(episodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving episodes for movie with slug {slug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving episodes.");
            }
        }

        // GET: api/movies/{slug}/episodes/{episodeId}
        [HttpGet("{slug}/episodes/{episodeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EpisodeViewModel>> GetEpisode(string slug, string episodeId)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(episodeId))
            {
                _logger.LogWarning("Invalid slug or episodeId parameter provided.");
                return BadRequest("Slug and episodeId are required.");
            }

            try
            {
                var episode = await _streamingService.GetEpisodeAsync(slug, episodeId);
                if (episode == null)
                {
                    _logger.LogWarning($"Episode {episodeId} not found for movie with slug {slug}.");
                    return NotFound($"Episode {episodeId} not found for movie with slug {slug}.");
                }

                return Ok(episode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving episode {episodeId} for movie with slug {slug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the episode.");
            }
        }

        // POST: api/movies/{slug}/views
        [HttpPost("{slug}/views")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> IncrementViewCount(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Invalid slug parameter provided.");
                return BadRequest("Slug is required.");
            }

            try
            {
                await _streamingService.IncrementViewCountAsync(slug);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing view count for movie with slug {slug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while incrementing view count.");
            }
        }

        // POST: api/movies/cache
        [HttpPost("cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CachedMovie>> CacheMovie([FromBody] MovieDetailResponse movieDetail)
        {
            if (movieDetail == null || string.IsNullOrWhiteSpace(movieDetail.Slug))
            {
                _logger.LogWarning("Invalid movie detail data provided.");
                return BadRequest("Movie detail or slug is required.");
            }

            try
            {
                var cachedMovie = await _streamingService.CacheMovieAsync(movieDetail);
                return Ok(cachedMovie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error caching movie with slug {movieDetail.Slug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while caching the movie.");
            }
        }
    }
}