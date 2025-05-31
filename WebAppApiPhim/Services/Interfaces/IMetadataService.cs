using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IMetadataService
    {
        // Basic string list methods (for simple dropdowns)
        Task<List<string>> GetGenresAsync();
        Task<List<string>> GetCountriesAsync();
        Task<List<string>> GetMovieTypesAsync();

        // Full entity methods (for detailed metadata)
        Task<List<MovieGenre>> GetAllGenresAsync();
        Task<List<MovieCountry>> GetAllCountriesAsync();
        Task<List<MovieType>> GetAllMovieTypesAsync();

        // CRUD operations
        Task<bool> AddGenreAsync(string name, string slug, string description = null);
        Task<bool> AddCountryAsync(string name, string code, string slug);
        Task<bool> AddMovieTypeAsync(string name, string slug, string description = null);

        // Existence checks
        Task<bool> GenreExistsAsync(string name);
        Task<bool> CountryExistsAsync(string name);

        // Legacy methods (keep for compatibility)
        Task UpdateMetadataAsync(MovieDetailResponse movie, string slug);
        Task<string> GetMetadataAsync(string key);
    }
}
