using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingMovies([FromQuery] int limit = 10)
        {
            try
            {
                var trendingMovies = await _analyticsService.GetTrendingMoviesAsync(limit);
                return Ok(trendingMovies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending movies");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách phim xu hướng" });
            }
        }

        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularMovies([FromQuery] int limit = 10)
        {
            try
            {
                var popularMovies = await _analyticsService.GetPopularMoviesAsync(limit);
                return Ok(popularMovies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular movies");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách phim phổ biến" });
            }
        }

        [Authorize]
        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 10)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var recommendations = await _analyticsService.GetRecommendationsAsync(userId, limit);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách phim đề xuất" });
            }
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedMovies([FromQuery] string category = "home", [FromQuery] int limit = 10)
        {
            try
            {
                var featuredMovies = await _analyticsService.GetFeaturedMoviesAsync(category, limit);
                return Ok(featuredMovies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting featured movies for category {category}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách phim nổi bật" });
            }
        }

        [HttpGet("statistics/{slug}")]
        public async Task<IActionResult> GetMovieStatistics(string slug)
        {
            try
            {
                var statistics = await _analyticsService.GetMovieStatisticsAsync(slug);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting statistics for movie {slug}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thống kê phim" });
            }
        }
    }
}