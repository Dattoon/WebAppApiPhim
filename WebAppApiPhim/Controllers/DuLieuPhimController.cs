using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Controllers
{
    [Route("api/dulieuphim")]
    [ApiController]
    public class DuLieuPhimController : ControllerBase
    {
        private readonly IDuLieuPhimService _duLieuPhimService;
        private readonly ILogger<DuLieuPhimController> _logger;

        public DuLieuPhimController(IDuLieuPhimService duLieuPhimService, ILogger<DuLieuPhimController> logger)
        {
            _duLieuPhimService = duLieuPhimService;
            _logger = logger;
        }

        [HttpGet("movies")]
        public async Task<IActionResult> GetLatestMovies(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string version = "v1")
        {
            try
            {
                var result = await _duLieuPhimService.GetLatestMoviesAsync(page, limit, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLatestMovies");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("movies/{slug}")]
        public async Task<IActionResult> GetMovieDetail(
            string slug,
            [FromQuery] string version = "v1")
        {
            try
            {
                var result = await _duLieuPhimService.GetMovieDetailBySlugAsync(slug, version);

                if (string.IsNullOrEmpty(result.Slug))
                {
                    return NotFound(new { message = $"Movie with slug '{slug}' not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMovieDetail for slug {Slug}", slug);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("tmdb/{slug}")]
        public async Task<IActionResult> GetTmdbData(string slug)
        {
            try
            {
                var result = await _duLieuPhimService.GetTmdbBySlugAsync(slug);

                if (result.Status != "success" || result.Data == null)
                {
                    return NotFound(new { message = $"TMDB data for slug '{slug}' not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTmdbData for slug {Slug}", slug);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("actors/{slug}")]
        public async Task<IActionResult> GetActors(string slug)
        {
            try
            {
                var result = await _duLieuPhimService.GetActorsBySlugAsync(slug);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActors for slug {Slug}", slug);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("production/{slug}")]
        public async Task<IActionResult> GetProduction(string slug)
        {
            try
            {
                var result = await _duLieuPhimService.GetProductionBySlugAsync(slug);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProduction for slug {Slug}", slug);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("images/{slug}")]
        public async Task<IActionResult> GetImages(
            string slug,
            [FromQuery] string version = "v1")
        {
            try
            {
                var result = await _duLieuPhimService.GetImagesBySlugAsync(slug, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetImages for slug {Slug}", slug);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterMovies(
            [FromQuery] string? name = null,
            [FromQuery] string? type = null,
            [FromQuery] string? genre = null,
            [FromQuery] string? country = null,
            [FromQuery] string? year = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                var result = await _duLieuPhimService.FilterMoviesAsync(name, type, genre, country, year, page, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FilterMovies");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("test")]
        public IActionResult TestConnection()
        {
            return Ok(new
            {
                message = "DuLieuPhim API service is working",
                timestamp = DateTime.UtcNow,
                endpoints = new[] {
                    "/api/dulieuphim/movies",
                    "/api/dulieuphim/movies/{slug}",
                    "/api/dulieuphim/tmdb/{slug}",
                    "/api/dulieuphim/actors/{slug}",
                    "/api/dulieuphim/production/{slug}",
                    "/api/dulieuphim/images/{slug}",
                    "/api/dulieuphim/filter"
                }
            });
        }
    }
}