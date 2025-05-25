using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestRealApiController : ControllerBase
    {
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<TestRealApiController> _logger;

        public TestRealApiController(
            IMovieApiService movieApiService,
            ILogger<TestRealApiController> logger)
        {
            _movieApiService = movieApiService;
            _logger = logger;
        }

        [HttpGet("test-all-versions")]
        public async Task<IActionResult> TestAllVersions()
        {
            try
            {
                var results = new Dictionary<string, object>();

                // Test V1
                var v1Result = await _movieApiService.GetLatestMoviesAsync(1, 3, "v1");
                results["v1"] = new
                {
                    success = v1Result.Data.Any(),
                    count = v1Result.Data.Count,
                    movies = v1Result.Data.Take(2).Select(m => new { m.Slug, m.Title, m.Year, m.TmdbId }),
                    pagination = v1Result.Pagination
                };

                // Test V2
                var v2Result = await _movieApiService.GetLatestMoviesAsync(1, 3, "v2");
                results["v2"] = new
                {
                    success = v2Result.Data.Any(),
                    count = v2Result.Data.Count,
                    movies = v2Result.Data.Take(2).Select(m => new { m.Slug, m.Title, m.Year, m.TmdbId }),
                    pagination = v2Result.Pagination
                };

                // Test V3
                var v3Result = await _movieApiService.GetLatestMoviesAsync(1, 3, "v3");
                results["v3"] = new
                {
                    success = v3Result.Data.Any(),
                    count = v3Result.Data.Count,
                    movies = v3Result.Data.Take(2).Select(m => new { m.Slug, m.Title, m.Year, m.TmdbId }),
                    pagination = v3Result.Pagination
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("test-movie-detail/{slug}")]
        public async Task<IActionResult> TestMovieDetail(string slug)
        {
            try
            {
                var detail = await _movieApiService.GetMovieDetailBySlugAsync(slug, "v1");

                if (detail == null)
                {
                    return NotFound(new { message = "Movie not found" });
                }

                return Ok(new
                {
                    success = true,
                    slug = detail.Slug,
                    title = detail.Title,
                    description = detail.Description?.Substring(0),
                    posterUrl = detail.PosterUrl,
                    thumbUrl = detail.ThumbUrl,
                    year = detail.Year,
                    director = detail.Director,
                    episodeCount = detail.Episodes?.Count ?? 0,
                    genres = detail.Genres,
                    countries = detail.Countries
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("test-images/{slug}")]
        public async Task<IActionResult> TestImages(string slug)
        {
            try
            {
                var images = await _movieApiService.GetMovieImagesAsync(slug);
                return Ok(images);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("test-search")]
        public async Task<IActionResult> TestSearch()
        {
            try
            {
                var results = new Dictionary<string, object>();

                // Test search by genre
                var genreResult = await _movieApiService.GetMoviesByGenreAsync("Tình Cảm", 1);
                results["byGenre"] = new
                {
                    success = genreResult.Data.Any(),
                    count = genreResult.Data.Count,
                    movies = genreResult.Data.Take(2).Select(m => new { m.Slug, m.Title })
                };

                // Test search by country
                var countryResult = await _movieApiService.GetMoviesByCountryAsync("Hàn Quốc", 1);
                results["byCountry"] = new
                {
                    success = countryResult.Data.Any(),
                    count = countryResult.Data.Count,
                    movies = countryResult.Data.Take(2).Select(m => new { m.Slug, m.Title })
                };

                // Test search by type
                var typeResult = await _movieApiService.GetMoviesByTypeAsync("Phim lẻ", 1);
                results["byType"] = new
                {
                    success = typeResult.Data.Any(),
                    count = typeResult.Data.Count,
                    movies = typeResult.Data.Take(2).Select(m => new { m.Slug, m.Title })
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
