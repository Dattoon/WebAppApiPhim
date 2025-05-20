using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamingController : ControllerBase
    {
        private readonly IMovieApiService _movieService;
        private readonly IUserService _userService;
        private readonly IStreamingService _streamingService;
        private readonly ILogger<StreamingController> _logger;

        public StreamingController(
            IMovieApiService movieService,
            IUserService userService,
            IStreamingService streamingService,
            ILogger<StreamingController> logger)
        {
            _movieService = movieService;
            _userService = userService;
            _streamingService = streamingService;
            _logger = logger;
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetMovieDetails(string slug)
        {
            try
            {
                var movieDetail = await _movieService.GetMovieDetailBySlugAsync(slug);
                if (movieDetail == null)
                    return NotFound(new { message = "Không tìm thấy phim" });

                // Lấy thông tin từ cache nếu có
                var cachedMovie = await _streamingService.GetCachedMovieAsync(slug);

                // Nếu chưa có trong cache, lưu vào cache
                if (cachedMovie == null)
                {
                    cachedMovie = await _streamingService.CacheMovieAsync(movieDetail);
                }

                // Lấy thông tin người dùng nếu đã đăng nhập
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isFavorite = false;
                Rating userRating = null;
                WatchHistory lastWatched = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    isFavorite = await _userService.IsFavoriteAsync(userId, slug);
                    userRating = await _userService.GetUserRatingAsync(userId, slug);
                    lastWatched = await _userService.GetLastWatchedAsync(userId, slug);
                }

                // Lấy phim liên quan
                var relatedMovies = await _movieService.GetRelatedMoviesAsync(slug, 6);

                // Tạo response
                var response = new
                {
                    movie = cachedMovie,
                    episodes = await _streamingService.GetEpisodesAsync(slug),
                    isFavorite,
                    userRating = userRating?.Value,
                    lastWatched = lastWatched != null ? new
                    {
                        episodeSlug = lastWatched.EpisodeSlug,
                        episodeName = lastWatched.EpisodeName,
                        watchedPercentage = lastWatched.WatchedPercentage,
                        currentTime = lastWatched.CurrentTime,
                        watchedAt = lastWatched.WatchedAt
                    } : null,
                    relatedMovies = relatedMovies.Data
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting movie details for {slug}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin phim" });
            }
        }

        [HttpGet("{slug}/episodes")]
        public async Task<IActionResult> GetEpisodes(string slug)
        {
            try
            {
                var episodes = await _streamingService.GetEpisodesAsync(slug);
                if (episodes == null || !episodes.Any())
                    return NotFound(new { message = "Không tìm thấy tập phim" });

                return Ok(episodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episodes for {slug}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách tập phim" });
            }
        }

        [HttpGet("{slug}/episodes/{episodeSlug}")]
        public async Task<IActionResult> GetEpisode(string slug, string episodeSlug)
        {
            try
            {
                var episode = await _streamingService.GetEpisodeAsync(slug, episodeSlug);
                if (episode == null)
                    return NotFound(new { message = "Không tìm thấy tập phim" });

                // Tăng lượt xem
                await _streamingService.IncrementViewCountAsync(slug);

                // Lấy thông tin người dùng nếu đã đăng nhập
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                EpisodeProgress progress = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    progress = await _streamingService.GetEpisodeProgressAsync(userId, slug, episodeSlug);
                }

                return Ok(new
                {
                    episode,
                    progress = progress != null ? new
                    {
                        currentTime = progress.CurrentTime,
                        duration = progress.Duration,
                        watchedPercentage = progress.CurrentTime / progress.Duration * 100
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episode {episodeSlug} for movie {slug}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin tập phim" });
            }
        }

        [Authorize]
        [HttpPost("{slug}/episodes/{episodeSlug}/progress")]
        public async Task<IActionResult> UpdateProgress(string slug, string episodeSlug, [FromBody] WatchProgressRequest request)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Cập nhật tiến độ xem
                await _streamingService.UpdateEpisodeProgressAsync(
                    userId,
                    slug,
                    episodeSlug,
                    request.CurrentTime,
                    request.Duration);

                // Cập nhật lịch sử xem
                await _userService.AddToWatchHistoryAsync(
                    userId,
                    slug,
                    request.MovieName,
                    request.MoviePosterUrl,
                    episodeSlug,
                    request.EpisodeName,
                    request.CurrentTime / request.Duration * 100);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating progress for episode {episodeSlug} of movie {slug}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật tiến độ xem" });
            }
        }
    }

    public class WatchProgressRequest
    {
        public string MovieName { get; set; }
        public string MoviePosterUrl { get; set; }
        public string EpisodeName { get; set; }
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
    }
}