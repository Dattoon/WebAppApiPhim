using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IStreamingService
    {
        Task<List<EpisodeViewModel>> GetEpisodesAsync(string movieSlug);
        Task<EpisodeViewModel> GetEpisodeAsync(string movieSlug, string episodeId);
        Task<List<ServerViewModel>> GetServersAsync(string movieSlug, int episodeNumber);
        Task<string> GetStreamingUrlAsync(string movieSlug, int episodeNumber, string serverName);
        Task<bool> ValidateStreamingUrlAsync(string url);
        Task<CachedMovie> GetCachedMovieAsync(string slug);
        Task IncrementViewCountAsync(string slug);
        Task<CachedMovie> CacheMovieAsync(MovieDetailResponse movieDetail);
        Task<EpisodeProgress> GetEpisodeProgressAsync(string userId, string movieSlug, string episodeId);
        Task UpdateEpisodeProgressAsync(string userId, string movieSlug, string episodeId, double currentTime, double duration);
    }
}

