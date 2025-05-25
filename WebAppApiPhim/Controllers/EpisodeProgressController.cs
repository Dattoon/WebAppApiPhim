using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebAppApiPhim.Services.Interfaces;
using WebAppApiPhim.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EpisodeProgressController : ControllerBase
    {
        private readonly IStreamingService _streamingService;
        private readonly ILogger<EpisodeProgressController> _logger;

        public EpisodeProgressController(IStreamingService streamingService, ILogger<EpisodeProgressController> logger)
        {
            _streamingService = streamingService ?? throw new ArgumentNullException(nameof(streamingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{movieSlug}/{episodeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EpisodeProgress>> GetEpisodeProgress(string movieSlug, string episodeId)
        {
            if (string.IsNullOrWhiteSpace(movieSlug) || string.IsNullOrWhiteSpace(episodeId))
            {
                _logger.LogWarning("Invalid movieSlug or episodeId parameters provided.");
                return BadRequest("MovieSlug and episodeId are required.");
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var progress = await _streamingService.GetEpisodeProgressAsync(userId, movieSlug, episodeId);
                if (progress == null)
                {
                    _logger.LogWarning($"No progress found for movie {movieSlug}, episode {episodeId}.");
                    return NotFound($"No progress found for movie {movieSlug}, episode {episodeId}.");
                }

                return Ok(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving episode progress for movie {movieSlug}, episode {episodeId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving episode progress.");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEpisodeProgress(
            [FromQuery] string movieSlug,
            [FromQuery] string episodeId,
            [FromQuery] double currentTime,
            [FromQuery] double duration)
        {
            if (string.IsNullOrWhiteSpace(movieSlug) || string.IsNullOrWhiteSpace(episodeId))
            {
                _logger.LogWarning("Invalid movieSlug or episodeId parameters provided.");
                return BadRequest("MovieSlug and episodeId are required.");
            }

            if (currentTime < 0 || duration <= 0)
            {
                _logger.LogWarning("Invalid currentTime or duration parameters provided.");
                return BadRequest("Invalid currentTime or duration.");
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                await _streamingService.UpdateEpisodeProgressAsync(userId, movieSlug, episodeId, currentTime, duration);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating episode progress for movie {movieSlug}, episode {episodeId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating episode progress.");
            }
        }
    }
}