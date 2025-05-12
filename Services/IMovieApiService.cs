using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IMovieApiService
    {
        Task<MovieListResponse> GetNewMoviesAsync(int page = 1, int limit = 10);
        Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug);
        Task<MovieListResponse> SearchMoviesAsync(string keyword, int page = 1);
        Task<MovieListResponse> GetMoviesByCategoryAsync(string category, int page = 1);
        Task<MovieListResponse> GetMoviesByCountryAsync(string country, int page = 1);
        Task<MovieListResponse> GetMoviesByTypeAsync(string type, int page = 1);
        Task<MovieListResponse> GetRelatedMoviesAsync(string slug, int limit = 6);
        Task<MovieListResponse> GetTrendingMoviesAsync(int page = 1, int limit = 10);
    }
}