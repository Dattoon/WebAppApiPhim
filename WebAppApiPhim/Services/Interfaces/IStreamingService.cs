using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IStreamingService
    {
        Task<CachedMovie> GetCachedMovieAsync(string slug);
        Task<CachedMovie> CacheMovieAsync(MovieDetailResponse movieDetail);
        Task<List<ServerViewModel>> GetEpisodesAsync(string slug);
        Task<EpisodeViewModel> GetEpisodeAsync(string slug, string episodeSlug);
        Task<EpisodeProgress> GetEpisodeProgressAsync(string userId, string movieSlug, string episodeSlug);
        Task UpdateEpisodeProgressAsync(string userId, string movieSlug, string episodeSlug, double currentTime, double duration);
        Task IncrementViewCountAsync(string slug);
    }
}
