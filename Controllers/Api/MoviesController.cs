using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieApiService _movieApiService;

        public MoviesController(IMovieApiService movieApiService)
        {
            _movieApiService = movieApiService;
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestMovies([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            var movies = await _movieApiService.GetLatestMoviesAsync(page, limit);
            return Ok(movies);
        }

        [HttpGet("detail/{slug}")]
        public async Task<IActionResult> GetMovieDetail(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return BadRequest("Slug is required");

            var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);
            if (movieDetail == null)
                return NotFound();

            return Ok(movieDetail);
        }

        [HttpGet("related/{slug}")]
        public async Task<IActionResult> GetRelatedMovies(string slug, [FromQuery] int limit = 6)
        {
            if (string.IsNullOrEmpty(slug))
                return BadRequest("Slug is required");

            var relatedMovies = await _movieApiService.GetRelatedMoviesAsync(slug, limit);
            return Ok(relatedMovies);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMovies([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrEmpty(query))
                return BadRequest("Query is required");

            var searchResults = await _movieApiService.SearchMoviesAsync(query, page, limit);
            return Ok(searchResults);
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
            var filterResults = await _movieApiService.FilterMoviesAsync(type, genre, country, year, page, limit);
            return Ok(filterResults);
        }

        [HttpGet("genres")]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _movieApiService.GetGenresAsync();
            return Ok(genres);
        }

        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            var countries = await _movieApiService.GetCountriesAsync();
            return Ok(countries);
        }

        [HttpGet("years")]
        public async Task<IActionResult> GetYears()
        {
            var years = await _movieApiService.GetYearsAsync();
            return Ok(years);
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetMovieTypes()
        {
            var types = await _movieApiService.GetMovieTypesAsync();
            return Ok(types);
        }
    }
}
