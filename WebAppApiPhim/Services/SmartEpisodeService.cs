using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface ISmartEpisodeService
    {
        Task<List<CachedEpisode>> GetMovieEpisodesAsync(string movieSlug);
        Task<CachedEpisode> GetEpisodeAsync(string movieSlug, int episodeNumber);
        Task<List<EpisodeServer>> GetEpisodeServersAsync(string episodeId);
        Task<CachedEpisode> AddEpisodeAsync(string movieSlug, int episodeNumber, string title, List<SmartServerData> servers);
        Task<List<CachedEpisode>> ParseEpisodeDataAsync(string movieSlug, string episodeData);
        Task<SmartStreamingInfo> GetBestStreamingInfoAsync(string episodeId, string preferredQuality = "HD");
        Task<List<SmartServerInfo>> AnalyzeEpisodeServersAsync(string episodeId);
    }

    public class SmartEpisodeService : ISmartEpisodeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SmartEpisodeService> _logger;

        public SmartEpisodeService(ApplicationDbContext context, ILogger<SmartEpisodeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CachedEpisode>> GetMovieEpisodesAsync(string movieSlug)
        {
            try
            {
                return await _context.CachedEpisodes
                    .Where(e => e.MovieSlug == movieSlug)
                    .Include(e => e.EpisodeServers)
                    .OrderBy(e => e.EpisodeNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episodes for movie: {movieSlug}", movieSlug);
                return new List<CachedEpisode>();
            }
        }

        public async Task<CachedEpisode> GetEpisodeAsync(string movieSlug, int episodeNumber)
        {
            try
            {
                return await _context.CachedEpisodes
                    .Where(e => e.MovieSlug == movieSlug && e.EpisodeNumber == episodeNumber)
                    .Include(e => e.EpisodeServers)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episode {episodeNumber} for movie: {movieSlug}", episodeNumber, movieSlug);
                return null;
            }
        }

        public async Task<List<EpisodeServer>> GetEpisodeServersAsync(string episodeId)
        {
            try
            {
                return await _context.EpisodeServers
                    .Where(s => s.EpisodeId == episodeId)
                    .OrderBy(s => s.ServerName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting servers for episode: {episodeId}", episodeId);
                return new List<EpisodeServer>();
            }
        }

        public async Task<CachedEpisode> AddEpisodeAsync(string movieSlug, int episodeNumber, string title, List<SmartServerData> servers)
        {
            try
            {
                // Kiểm tra episode đã tồn tại
                var existingEpisode = await _context.CachedEpisodes
                    .Include(e => e.EpisodeServers)
                    .FirstOrDefaultAsync(e => e.MovieSlug == movieSlug && e.EpisodeNumber == episodeNumber);

                if (existingEpisode != null)
                {
                    // Cập nhật servers cho episode hiện có
                    return await UpdateEpisodeServersAsync(existingEpisode, servers);
                }

                // Tạo episode mới
                var episode = new CachedEpisode
                {
                    Id = Guid.NewGuid().ToString(),
                    MovieSlug = movieSlug,
                    EpisodeNumber = episodeNumber,
                    Title = title ?? $"Tập {episodeNumber}",
                    Url = servers?.FirstOrDefault()?.Url ?? "", // URL chính
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CachedEpisodes.Add(episode);
                await _context.SaveChangesAsync();

                // Thêm servers
                if (servers?.Any() == true)
                {
                    await AddEpisodeServersAsync(episode.Id, servers);
                }

                return episode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding episode {episodeNumber} for movie: {movieSlug}", episodeNumber, movieSlug);
                throw;
            }
        }

        public async Task<List<CachedEpisode>> ParseEpisodeDataAsync(string movieSlug, string episodeData)
        {
            try
            {
                var episodes = new List<CachedEpisode>();
                var lines = episodeData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var match = Regex.Match(line.Trim(), @"Tập\s+(\d+)\|(.+)");
                    if (match.Success)
                    {
                        var episodeNumber = int.Parse(match.Groups[1].Value);
                        var url = match.Groups[2].Value.Trim();

                        var servers = AnalyzeUrlAndCreateServers(url);

                        var episode = await AddEpisodeAsync(movieSlug, episodeNumber, $"Tập {episodeNumber}", servers);
                        if (episode != null)
                        {
                            episodes.Add(episode);
                        }
                    }
                }

                return episodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing episode data for movie: {movieSlug}", movieSlug);
                return new List<CachedEpisode>();
            }
        }

        public async Task<SmartStreamingInfo> GetBestStreamingInfoAsync(string episodeId, string preferredQuality = "HD")
        {
            try
            {
                var episode = await _context.CachedEpisodes
                    .Include(e => e.EpisodeServers)
                    .FirstOrDefaultAsync(e => e.Id == episodeId);

                if (episode == null)
                    return new SmartStreamingInfo { Success = false, Message = "Episode not found" };

                var servers = await AnalyzeEpisodeServersAsync(episodeId);

                // Tìm server tốt nhất dựa trên quality và type
                var bestServer = servers
                    .Where(s => s.Quality == preferredQuality)
                    .OrderBy(s => GetServerTypePriority(s.Type))
                    .FirstOrDefault() ?? servers.FirstOrDefault();

                if (bestServer == null)
                    return new SmartStreamingInfo { Success = false, Message = "No servers available" };

                return new SmartStreamingInfo
                {
                    Success = true,
                    EpisodeId = episodeId,
                    EpisodeNumber = episode.EpisodeNumber,
                    Title = episode.Title,
                    PrimaryUrl = bestServer.Url,
                    StreamingType = bestServer.Type,
                    Quality = bestServer.Quality,
                    AllServers = servers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streaming info for episode: {episodeId}", episodeId);
                return new SmartStreamingInfo { Success = false, Message = "Error getting streaming info" };
            }
        }

        public async Task<List<SmartServerInfo>> AnalyzeEpisodeServersAsync(string episodeId)
        {
            try
            {
                var servers = await GetEpisodeServersAsync(episodeId);
                var smartServers = new List<SmartServerInfo>();

                foreach (var server in servers)
                {
                    var analysis = AnalyzeServerUrl(server.ServerUrl);

                    smartServers.Add(new SmartServerInfo
                    {
                        Id = server.Id,
                        Name = server.ServerName,
                        Url = server.ServerUrl,
                        Type = analysis.Type,
                        Quality = analysis.Quality,
                        IsWorking = await CheckServerStatusAsync(server.ServerUrl),
                        Priority = GetServerTypePriority(analysis.Type)
                    });
                }

                return smartServers.OrderBy(s => s.Priority).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing servers for episode: {episodeId}", episodeId);
                return new List<SmartServerInfo>();
            }
        }

        #region Private Helper Methods

        private List<SmartServerData> AnalyzeUrlAndCreateServers(string url)
        {
            var servers = new List<SmartServerData>();

            if (url.Contains("player.phimapi.com"))
            {
                // Embed player
                servers.Add(new SmartServerData
                {
                    Name = "Embed Player",
                    Url = url,
                    Type = "embed",
                    Quality = "HD"
                });

                // Trích xuất M3U8 từ embed URL
                var m3u8Match = Regex.Match(url, @"url=(.+)");
                if (m3u8Match.Success)
                {
                    var m3u8Url = Uri.UnescapeDataString(m3u8Match.Groups[1].Value);
                    servers.Add(new SmartServerData
                    {
                        Name = "Direct M3U8",
                        Url = m3u8Url,
                        Type = "m3u8",
                        Quality = "HD"
                    });
                }
            }
            else if (url.Contains(".m3u8"))
            {
                servers.Add(new SmartServerData
                {
                    Name = "M3U8 Stream",
                    Url = url,
                    Type = "m3u8",
                    Quality = DetermineQualityFromUrl(url)
                });
            }
            else
            {
                servers.Add(new SmartServerData
                {
                    Name = "Default Server",
                    Url = url,
                    Type = "mp4",
                    Quality = DetermineQualityFromUrl(url)
                });
            }

            return servers;
        }

        private ServerAnalysis AnalyzeServerUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return new ServerAnalysis { Type = "unknown", Quality = "SD" };

            var type = "mp4"; // default
            if (url.Contains(".m3u8")) type = "m3u8";
            else if (url.Contains("embed") || url.Contains("player")) type = "embed";

            var quality = DetermineQualityFromUrl(url);

            return new ServerAnalysis { Type = type, Quality = quality };
        }

        private string DetermineQualityFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "SD";

            if (url.Contains("1080p") || url.Contains("fullhd")) return "FHD";
            if (url.Contains("720p") || url.Contains("hd")) return "HD";
            if (url.Contains("480p")) return "SD";
            if (url.Contains("360p")) return "SD";

            return "HD"; // Default assumption
        }

        private int GetServerTypePriority(string type)
        {
            return type switch
            {
                "m3u8" => 1,    // Ưu tiên cao nhất
                "mp4" => 2,     // Ưu tiên trung bình
                "embed" => 3,   // Ưu tiên thấp nhất
                _ => 4          // Unknown
            };
        }

        private async Task<bool> CheckServerStatusAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                // Sử dụng SendAsync với HttpMethod.Head thay vì HeadAsync
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false; // Assume not working if can't check
            }
        }

        private async Task<CachedEpisode> UpdateEpisodeServersAsync(CachedEpisode episode, List<SmartServerData> servers)
        {
            // Xóa servers cũ
            _context.EpisodeServers.RemoveRange(episode.EpisodeServers);

            // Thêm servers mới
            await AddEpisodeServersAsync(episode.Id, servers);

            // Cập nhật URL chính
            var primaryServer = servers?.FirstOrDefault();
            if (primaryServer != null)
            {
                episode.Url = primaryServer.Url;
                episode.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return episode;
        }

        private async Task AddEpisodeServersAsync(string episodeId, List<SmartServerData> servers)
        {
            foreach (var serverData in servers)
            {
                var server = new EpisodeServer
                {
                    Id = Guid.NewGuid().ToString(),
                    EpisodeId = episodeId,
                    ServerName = $"{serverData.Name} ({serverData.Type.ToUpper()}) - {serverData.Quality}",
                    ServerUrl = serverData.Url
                };

                _context.EpisodeServers.Add(server);
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }

    // Helper Classes
    public class SmartServerData
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Type { get; set; } // "embed", "m3u8", "mp4"
        public string Quality { get; set; } = "HD";
    }

    public class SmartServerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string Quality { get; set; }
        public bool IsWorking { get; set; }
        public int Priority { get; set; }
    }

    public class SmartStreamingInfo
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string EpisodeId { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public string PrimaryUrl { get; set; }
        public string StreamingType { get; set; }
        public string Quality { get; set; }
        public List<SmartServerInfo> AllServers { get; set; } = new();
    }

    public class ServerAnalysis
    {
        public string Type { get; set; }
        public string Quality { get; set; }
    }
}
