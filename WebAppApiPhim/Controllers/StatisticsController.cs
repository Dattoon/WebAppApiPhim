using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(
            IStatisticsService statisticsService,
            ILogger<StatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        /// <summary>
        /// Get general statistics
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<StatisticsViewModel>>> GetStatistics()
        {
            try
            {
                var statistics = await _statisticsService.GetStatisticsAsync();

                return Ok(new ApiResponse<StatisticsViewModel>
                {
                    Success = true,
                    Message = "Statistics retrieved successfully",
                    Data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return StatusCode(500, new ApiResponse<StatisticsViewModel>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get popular movies
        /// </summary>
        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse<System.Collections.Generic.List<MovieListItemViewModel>>>> GetPopularMovies(
            [FromQuery] int count = 10)
        {
            try
            {
                if (count < 1 || count > 50) count = 10;

                var movies = await _statisticsService.GetPopularMoviesAsync(count);

                return Ok(new ApiResponse<System.Collections.Generic.List<MovieListItemViewModel>>
                {
                    Success = true,
                    Message = "Popular movies retrieved successfully",
                    Data = movies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular movies");
                return StatusCode(500, new ApiResponse<System.Collections.Generic.List<MovieListItemViewModel>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get recent movies
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<ApiResponse<System.Collections.Generic.List<MovieListItemViewModel>>>> GetRecentMovies(
            [FromQuery] int count = 10)
        {
            try
            {
                if (count < 1 || count > 50) count = 10;

                var movies = await _statisticsService.GetRecentMoviesAsync(count);

                return Ok(new ApiResponse<System.Collections.Generic.List<MovieListItemViewModel>>
                {
                    Success = true,
                    Message = "Recent movies retrieved successfully",
                    Data = movies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent movies");
                return StatusCode(500, new ApiResponse<System.Collections.Generic.List<MovieListItemViewModel>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }
    }
}
