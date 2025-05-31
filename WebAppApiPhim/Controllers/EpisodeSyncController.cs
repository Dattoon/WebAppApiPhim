using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [Route("api/episode-sync")]
    [ApiController]
    public class EpisodeSyncController : ControllerBase
    {
        private readonly IEpisodeSyncService _episodeSyncService;
        private readonly ILogger<EpisodeSyncController> _logger;

        public EpisodeSyncController(
            IEpisodeSyncService episodeSyncService,
            ILogger<EpisodeSyncController> logger)
        {
            _episodeSyncService = episodeSyncService;
            _logger = logger;
        }

        [HttpPost("movie/{movieSlug}")]
        public async Task<IActionResult> SyncEpisodesForMovie(string movieSlug)
        {
            try
            {
                var (added, updated, failed) = await _episodeSyncService.SyncEpisodesForMovieAsync(movieSlug);

                return Ok(new
                {
                    message = $"Synced episodes for movie {movieSlug}",
                    added,
                    updated,
                    failed,
                    total = added + updated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing episodes for movie: {movieSlug}", movieSlug);
                return StatusCode(500, new { message = "Error syncing episodes", error = ex.Message });
            }
        }

        [HttpPost("recent")]
        public async Task<IActionResult> SyncEpisodesForRecentMovies([FromQuery] int count = 10)
        {
            try
            {
                var (added, updated, failed) = await _episodeSyncService.SyncEpisodesForRecentMoviesAsync(count);

                return Ok(new
                {
                    message = $"Synced episodes for {count} recent movies",
                    added,
                    updated,
                    failed,
                    total = added + updated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing episodes for recent movies");
                return StatusCode(500, new { message = "Error syncing episodes", error = ex.Message });
            }
        }
    }
}
