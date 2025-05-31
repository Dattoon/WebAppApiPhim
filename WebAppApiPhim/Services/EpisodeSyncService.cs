using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
    public interface IEpisodeSyncService
    {
        Task<(int added, int updated, int failed)> SyncEpisodesForMovieAsync(string movieSlug);
        Task<(int added, int updated, int failed)> SyncEpisodesForRecentMoviesAsync(int count = 10);
    }

    public class EpisodeSyncService : IEpisodeSyncService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMovieApiService _externalMovieApiService;
        private readonly ISmartEpisodeService _smartEpisodeService;
        private readonly ILogger<EpisodeSyncService> _logger;

        public EpisodeSyncService(
            ApplicationDbContext context,
            IMovieApiService externalMovieApiService,
            ISmartEpisodeService smartEpisodeService,
            ILogger<EpisodeSyncService> logger)
        {
            _context = context;
            _externalMovieApiService = externalMovieApiService;
            _smartEpisodeService = smartEpisodeService;
            _logger = logger;
        }

        public async Task<(int added, int updated, int failed)> SyncEpisodesForMovieAsync(string movieSlug)
        {
            int added = 0;
            int updated = 0;
            int failed = 0;

            try
            {
                _logger.LogInformation("Syncing episodes for movie: {slug}", movieSlug);

                // Kiểm tra phim có tồn tại trong database không
                var movie = await _context.CachedMovies
                    .Include(m => m.Episodes)
                    .FirstOrDefaultAsync(m => m.Slug == movieSlug);

                if (movie == null)
                {
                    _logger.LogWarning("Movie not found in database: {slug}", movieSlug);
                    return (0, 0, 0);
                }

                // Lấy thông tin chi tiết từ API bên ngoài
                var movieDetail = await _externalMovieApiService.GetMovieDetailBySlugAsync(movieSlug);
                if (movieDetail == null || movieDetail.Episodes == null || !movieDetail.Episodes.Any())
                {
                    _logger.LogWarning("No episodes found in external API for movie: {slug}", movieSlug);
                    return (0, 0, 0);
                }

                // Xử lý từng nhóm episode
                int episodeNumber = 1;
                foreach (var serverGroup in movieDetail.Episodes)
                {
                    foreach (var episodeItem in serverGroup.Items)
                    {
                        try
                        {
                            // Tạo danh sách servers
                            var servers = new List<SmartServerData>();

                            // Thêm embed URL nếu có
                            if (!string.IsNullOrEmpty(episodeItem.Embed))
                            {
                                servers.Add(new SmartServerData
                                {
                                    Name = serverGroup.ServerName ?? "Embed Player",
                                    Url = episodeItem.Embed,
                                    Type = "embed",
                                    Quality = "HD"
                                });
                            }

                            // Thêm m3u8 URL nếu có
                            if (!string.IsNullOrEmpty(episodeItem.M3u8))
                            {
                                servers.Add(new SmartServerData
                                {
                                    Name = serverGroup.ServerName ?? "M3U8 Stream",
                                    Url = episodeItem.M3u8,
                                    Type = "m3u8",
                                    Quality = "HD"
                                });
                            }

                            // Thêm URL thông thường nếu không có embed hoặc m3u8
                            if (servers.Count == 0 && !string.IsNullOrEmpty(episodeItem.Url))
                            {
                                servers.Add(new SmartServerData
                                {
                                    Name = serverGroup.ServerName ?? "Default Server",
                                    Url = episodeItem.Url,
                                    Type = DetermineUrlType(episodeItem.Url),
                                    Quality = "HD"
                                });
                            }

                            // Nếu không có server nào, bỏ qua episode này
                            if (servers.Count == 0)
                            {
                                _logger.LogWarning("No valid URLs found for episode {number} of movie {slug}", episodeNumber, movieSlug);
                                continue;
                            }

                            // Tạo hoặc cập nhật episode
                            var title = !string.IsNullOrEmpty(episodeItem.Title) ? episodeItem.Title : $"Tập {episodeNumber}";
                            var existingEpisode = movie.Episodes.FirstOrDefault(e => e.EpisodeNumber == episodeNumber);

                            if (existingEpisode == null)
                            {
                                await _smartEpisodeService.AddEpisodeAsync(movieSlug, episodeNumber, title, servers);
                                added++;
                                _logger.LogInformation("Added episode {number} for movie {slug}", episodeNumber, movieSlug);
                            }
                            else
                            {
                                // Cập nhật episode hiện có
                                var updatedEpisode = await _smartEpisodeService.AddEpisodeAsync(movieSlug, episodeNumber, title, servers);
                                updated++;
                                _logger.LogInformation("Updated episode {number} for movie {slug}", episodeNumber, movieSlug);
                            }

                            episodeNumber++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing episode {number} for movie {slug}", episodeNumber, movieSlug);
                            failed++;
                        }
                    }
                }

                return (added, updated, failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing episodes for movie: {slug}", movieSlug);
                return (added, updated, failed + 1);
            }
        }

        public async Task<(int added, int updated, int failed)> SyncEpisodesForRecentMoviesAsync(int count = 10)
        {
            int totalAdded = 0;
            int totalUpdated = 0;
            int totalFailed = 0;

            try
            {
                // Lấy danh sách phim gần đây nhất
                var recentMovies = await _context.CachedMovies
                    .OrderByDescending(m => m.LastUpdated)
                    .Take(count)
                    .ToListAsync();

                _logger.LogInformation("Syncing episodes for {count} recent movies", recentMovies.Count);

                foreach (var movie in recentMovies)
                {
                    try
                    {
                        var (added, updated, failed) = await SyncEpisodesForMovieAsync(movie.Slug);
                        totalAdded += added;
                        totalUpdated += updated;
                        totalFailed += failed;

                        // Delay để tránh quá tải API
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing episodes for movie: {slug}", movie.Slug);
                        totalFailed++;
                    }
                }

                _logger.LogInformation("Completed syncing episodes for recent movies. Added: {added}, Updated: {updated}, Failed: {failed}",
                    totalAdded, totalUpdated, totalFailed);

                return (totalAdded, totalUpdated, totalFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing episodes for recent movies");
                return (totalAdded, totalUpdated, totalFailed);
            }
        }

        private string DetermineUrlType(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "unknown";

            if (url.Contains(".m3u8"))
                return "m3u8";

            if (url.Contains("embed") || url.Contains("player"))
                return "embed";

            if (url.Contains(".mp4"))
                return "mp4";

            return "unknown";
        }
    }
}
