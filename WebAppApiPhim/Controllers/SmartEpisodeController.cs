using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [Route("api/smart-episodes")]
    [ApiController]
    public class SmartEpisodeController : ControllerBase
    {
        private readonly ISmartEpisodeService _smartEpisodeService;
        private readonly ILogger<SmartEpisodeController> _logger;

        public SmartEpisodeController(
            ISmartEpisodeService smartEpisodeService,
            ILogger<SmartEpisodeController> logger)
        {
            _smartEpisodeService = smartEpisodeService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả episodes của phim với thông tin server thông minh
        /// </summary>
        [HttpGet("movie/{movieSlug}")]
        public async Task<IActionResult> GetMovieEpisodes(string movieSlug)
        {
            try
            {
                var episodes = await _smartEpisodeService.GetMovieEpisodesAsync(movieSlug);

                var result = new List<object>();
                foreach (var episode in episodes)
                {
                    var servers = await _smartEpisodeService.AnalyzeEpisodeServersAsync(episode.Id);

                    result.Add(new
                    {
                        episode.Id,
                        episode.EpisodeNumber,
                        episode.Title,
                        episode.Url,
                        episode.CreatedAt,
                        episode.UpdatedAt,
                        ServersCount = servers.Count,
                        Servers = servers.Take(3), // Chỉ hiển thị 3 server đầu
                        BestServer = servers.FirstOrDefault()
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episodes for movie: {movieSlug}", movieSlug);
                return StatusCode(500, new { message = "Error retrieving episodes" });
            }
        }

        /// <summary>
        /// Lấy thông tin streaming tốt nhất cho episode
        /// </summary>
        [HttpGet("{episodeId}/streaming")]
        public async Task<IActionResult> GetBestStreaming(string episodeId, [FromQuery] string quality = "HD")
        {
            try
            {
                var streamingInfo = await _smartEpisodeService.GetBestStreamingInfoAsync(episodeId, quality);
                return Ok(streamingInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streaming info for episode: {episodeId}", episodeId);
                return StatusCode(500, new { message = "Error getting streaming info" });
            }
        }

        /// <summary>
        /// Phân tích tất cả servers của episode
        /// </summary>
        [HttpGet("{episodeId}/servers/analyze")]
        public async Task<IActionResult> AnalyzeServers(string episodeId)
        {
            try
            {
                var servers = await _smartEpisodeService.AnalyzeEpisodeServersAsync(episodeId);
                return Ok(new
                {
                    EpisodeId = episodeId,
                    TotalServers = servers.Count,
                    WorkingServers = servers.Count(s => s.IsWorking),
                    ServerTypes = servers.GroupBy(s => s.Type).Select(g => new { Type = g.Key, Count = g.Count() }),
                    Servers = servers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing servers for episode: {episodeId}", episodeId);
                return StatusCode(500, new { message = "Error analyzing servers" });
            }
        }

        /// <summary>
        /// Parse dữ liệu episode từ text
        /// </summary>
        [HttpPost("movie/{movieSlug}/parse")]
        public async Task<IActionResult> ParseEpisodeData(string movieSlug, [FromBody] ParseRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.EpisodeData))
                {
                    return BadRequest(new { message = "Episode data is required" });
                }

                var episodes = await _smartEpisodeService.ParseEpisodeDataAsync(movieSlug, request.EpisodeData);

                return Ok(new
                {
                    message = $"Successfully parsed {episodes.Count} episodes",
                    episodes = episodes.Select(e => new
                    {
                        e.Id,
                        e.EpisodeNumber,
                        e.Title,
                        e.Url,
                        ServersCount = e.EpisodeServers?.Count ?? 0
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing episode data for movie: {movieSlug}", movieSlug);
                return StatusCode(500, new { message = "Error parsing episode data" });
            }
        }

        /// <summary>
        /// Thêm episode mới với nhiều server
        /// </summary>
        [HttpPost("movie/{movieSlug}/episode")]
        public async Task<IActionResult> AddEpisode(string movieSlug, [FromBody] AddEpisodeRequest request)
        {
            try
            {
                var episode = await _smartEpisodeService.AddEpisodeAsync(
                    movieSlug,
                    request.EpisodeNumber,
                    request.Title,
                    request.Servers);

                return Ok(new
                {
                    message = "Episode added successfully",
                    episode = new
                    {
                        episode.Id,
                        episode.EpisodeNumber,
                        episode.Title,
                        episode.Url,
                        ServersCount = request.Servers?.Count ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding episode for movie: {movieSlug}", movieSlug);
                return StatusCode(500, new { message = "Error adding episode" });
            }
        }
    }

    // Request DTOs
    public class ParseRequest
    {
        public string EpisodeData { get; set; }
    }

    public class AddEpisodeRequest
    {
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public List<SmartServerData> Servers { get; set; } = new();
    }
}
