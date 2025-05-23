using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Repositories
{
    public interface IMetadataRepository
    {
        Task<List<Genre>> GetAllGenresAsync();
        Task<Genre> GetGenreByIdAsync(int id);
        Task<Genre> GetGenreBySlugAsync(string slug);
        Task<Genre> AddGenreAsync(Genre genre);
        Task<Genre> UpdateGenreAsync(Genre genre);
        Task<bool> DeleteGenreAsync(int id);

        Task<List<Country>> GetAllCountriesAsync();
        Task<Country> GetCountryByIdAsync(int id);
        Task<Country> GetCountryByCodeAsync(string code);
        Task<Country> AddCountryAsync(Country country);
        Task<Country> UpdateCountryAsync(Country country);
        Task<bool> DeleteCountryAsync(int id);

        Task<List<MovieType>> GetAllMovieTypesAsync();
        Task<MovieType> GetMovieTypeByIdAsync(int id);
        Task<MovieType> GetMovieTypeBySlugAsync(string slug);
        Task<MovieType> AddMovieTypeAsync(MovieType movieType);
        Task<MovieType> UpdateMovieTypeAsync(MovieType movieType);
        Task<bool> DeleteMovieTypeAsync(int id);

        Task UpdateMovieCountsAsync();
    }
}
