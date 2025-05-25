using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IStatisticsService
    {
        void InvalidateCache(string slug);
        Task<int> GetTotalMoviesAsync();
        Task<int> GetTotalUsersAsync();
        Task<int> GetMovieViewCountAsync(string movieSlug);
        Task<(string Quality, int Count)[]> GetMoviesByQualityAsync();
    }
}