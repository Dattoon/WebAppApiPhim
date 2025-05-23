using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamingController : ControllerBase
    {
        private readonly IStreamingService _streamingService;
        private readonly ILogger<StreamingController> _logger;

        public StreamingController(
            IStreamingService streamingService,
            ILogger<StreamingController> logger)
        {
            _streamingService = streamingService;
            _logger = logger;
        }

        /// <summary>
        /// Get episodes for a movie
        /// </summary>
        [HttpGet("{slug}/episodes")]
        public async Task<ActionResult<ApiResponse<List<ServerViewModel>>>> GetEpisodes(string slug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return BadRequest(new ApiResponse<List<ServerViewModel>>
                    {
                        Success = false,
                        Message = "Movie slug is required"
                    });
                }

                var episodes = await _streamingService.GetEpisodesAsync(slug);

                return Ok(new ApiResponse<List<ServerViewModel>>
                {
                    Success = true,
                    Message = "Episodes retrieved successfully",
                    Data = episodes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episodes for movie: {slug}");
                return StatusCode(500, new ApiResponse<List<ServerViewModel>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get specific episode
        /// </summary>
        [HttpGet("{slug}/episodes/{episodeSlug}")]
        public async Task<ActionResult<ApiResponse<EpisodeViewModel>>> GetEpisode(
            string slug,
            string episodeSlug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(episodeSlug))
                {
                    return BadRequest(new ApiResponse<EpisodeViewModel>
                    {
                        Success = false,
                        Message = "Movie slug and episode slug are required"
                    });
                }

                var episode = await _streamingService.GetEpisodeAsync(slug, episodeSlug);

                if (episode == null)
                {
                    return NotFound(new ApiResponse<EpisodeViewModel>
                    {
                        Success = false,
                        Message = "Episode not found"
                    });
                }

                return Ok(new ApiResponse<EpisodeViewModel>
                {
                    Success = true,
                    Message = "Episode retrieved successfully",
                    Data = episode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episode {episodeSlug} for movie: {slug}");
                return StatusCode(500, new ApiResponse<EpisodeViewModel>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get episode progress for authenticated user
        /// </summary>
        [HttpGet("{slug}/episodes/{episodeSlug}/progress")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<EpisodeProgress>>> GetEpisodeProgress(
            string slug,
            string episodeSlug)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<EpisodeProgress>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var progress = await _streamingService.GetEpisodeProgressAsync(userId, slug, episodeSlug);

                return Ok(new ApiResponse<EpisodeProgress>
                {
                    Success = true,
                    Message = "Episode progress retrieved successfully",
                    Data = progress
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episode progress for user");
                return StatusCode(500, new ApiResponse<EpisodeProgress>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Update episode progress for authenticated user
        /// </summary>
        [HttpPost("{slug}/episodes/{episodeSlug}/progress")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> UpdateEpisodeProgress(
            string slug,
            string episodeSlug,
            [FromBody] UpdateProgressRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                if (request == null || request.CurrentTime < 0 || request.Duration <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid progress data"
                    });
                }

                await _streamingService.UpdateEpisodeProgressAsync(
                    userId, slug, episodeSlug, request.CurrentTime, request.Duration);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Episode progress updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating episode progress");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Increment view count for a movie
        /// </summary>
        [HttpPost("{slug}/view")]
        public async Task<ActionResult<ApiResponse<object>>> IncrementViewCount(string slug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Movie slug is required"
                    });
                }

                await _streamingService.IncrementViewCountAsync(slug);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "View count updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing view count for movie: {slug}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }
    }

    // Request models
    public class UpdateProgressRequest
    {
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
    }
}
