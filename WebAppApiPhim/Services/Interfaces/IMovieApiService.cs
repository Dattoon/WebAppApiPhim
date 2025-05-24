using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IMovieApiService
    {
        Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = null);
        Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string version = null);
        Task<MovieListResponse> SearchMoviesAsync(string query, int page = 1, int limit = 10);
        Task<MovieListResponse> FilterMoviesAsync(string type = null, string genre = null, string country = null, string year = null, int page = 1, int limit = 10);
        Task<MovieListResponse> GetRelatedMoviesAsync(string slug, int limit = 6, string version = null);
        Task<List<string>> GetGenresAsync();
        Task<List<string>> GetCountriesAsync();
        Task<List<string>> GetYearsAsync();
        Task<List<string>> GetMovieTypesAsync();
    }
}
