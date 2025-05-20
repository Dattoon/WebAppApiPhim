using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IAnalyticsService
    {
        Task<List<MovieItem>> GetTrendingMoviesAsync(int limit = 10);
        Task<List<MovieItem>> GetPopularMoviesAsync(int limit = 10);
        Task<List<MovieItem>> GetRecommendationsAsync(string userId, int limit = 10);
        Task<List<MovieItem>> GetFeaturedMoviesAsync(string category = "home", int limit = 10);
        Task<MovieStatistic> GetMovieStatisticsAsync(string slug);
        Task UpdateMovieStatisticsAsync(string slug);
    }
}