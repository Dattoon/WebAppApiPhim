using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;


namespace WebAppApiPhim.Services.Interfaces
{
    public interface IMovieApiService
    {
        Task<List<string>> GetGenresAsync();
        Task<List<string>> GetCountriesAsync();
        Task<List<string>> GetMovieTypesAsync();
      
        Task<Models.MovieListResponse> GetMoviesByCategoryAsync(string category, int page = 1);
        Task<Models.MovieListResponse> GetMoviesByGenreAsync(string genreSlug, int page = 1);
        Task<Models.MovieListResponse> GetMoviesByCountryAsync(string countrySlug, int page = 1);
        Task<Models.MovieListResponse> GetMoviesByTypeAsync(string typeSlug, int page = 1);
        Task<Models.ImageResponse> GetMovieImagesAsync(string slug);
        Task<ProductionApiResponse> GetProductionDataAsync(string slug);

        Task<Models.MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 20, string version = null);
        Task<Models.MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string version = null);
    }
}