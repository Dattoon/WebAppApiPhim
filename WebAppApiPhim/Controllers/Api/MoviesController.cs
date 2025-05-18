using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAppApiPhim.Services;
using Microsoft.Extensions.Logging;

namespace WebAppApiPhim.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieApiService _movieService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(IMovieApiService movieService, ILogger<MoviesController> logger)
        {
            _movieService = movieService;
            _logger = logger;
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestMovies([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            _logger.LogInformation($"Getting latest movies: page={page}, limit={limit}");
            var result = await _movieService.GetLatestMoviesAsync(page, limit);
            return Ok(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetMovieBySlug(string slug)
        {
            _logger.LogInformation($"Getting movie details for slug: {slug}");
            var result = await _movieService.GetMovieDetailBySlugAsync(slug);

            if (result == null)
            {
                return NotFound(new { message = $"Movie with slug '{slug}' not found" });
            }

            return Ok(result);
        }

        [HttpGet("related/{slug}")]
        public async Task<IActionResult> GetRelatedMovies(string slug, [FromQuery] int limit = 6)
        {
            _logger.LogInformation($"Getting related movies for slug: {slug}, limit={limit}");
            var result = await _movieService.GetRelatedMoviesAsync(slug, limit);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMovies([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            _logger.LogInformation($"Searching movies: query={query}, page={page}, limit={limit}");
            var result = await _movieService.SearchMoviesAsync(query, page, limit);
            return Ok(result);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterMovies(
            [FromQuery] string type = null,
            [FromQuery] string genre = null,
            [FromQuery] string country = null,
            [FromQuery] string year = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            _logger.LogInformation($"Filtering movies: type={type}, genre={genre}, country={country}, year={year}, page={page}, limit={limit}");
            var result = await _movieService.FilterMoviesAsync(type, genre, country, year, page, limit);
            return Ok(result);
        }

        [HttpGet("genres")]
        public async Task<IActionResult> GetGenres()
        {
            var result = await _movieService.GetGenresAsync();
            return Ok(result);
        }

        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            var result = await _movieService.GetCountriesAsync();
            return Ok(result);
        }

        [HttpGet("years")]
        public async Task<IActionResult> GetYears()
        {
            var result = await _movieService.GetYearsAsync();
            return Ok(result);
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetMovieTypes()
        {
            var result = await _movieService.GetMovieTypesAsync();
            return Ok(result);
        }
    }
}