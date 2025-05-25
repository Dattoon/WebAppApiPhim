using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IStreamingService
    {
        Task<CachedMovie> GetCachedMovieAsync(string slug);
        Task<CachedMovie> CacheMovieAsync(MovieDetailResponse movieDetail);
        Task<List<ServerViewModel>> GetEpisodesAsync(string slug);
        Task<EpisodeViewModel> GetEpisodeAsync(string slug, string episodeId);
        Task<EpisodeProgress> GetEpisodeProgressAsync(string userId, string movieSlug, string episodeId);
        Task UpdateEpisodeProgressAsync(string userId, string movieSlug, string episodeId, double currentTime, double duration);
        Task IncrementViewCountAsync(string slug);
    }
}