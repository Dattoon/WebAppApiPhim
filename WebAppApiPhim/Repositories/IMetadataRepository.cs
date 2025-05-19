using WebAppApiPhim.Models;

namespace WebAppApiPhim.Repositories
{
    public interface IMetadataRepository
    {
        Task<List<MovieType>> GetMovieTypesAsync();
        Task<List<Genre>> GetGenresAsync();
        Task<List<Country>> GetCountriesAsync();
        Task<List<string>> GetYearsAsync(int startYear = 2000);

        Task<MovieType> GetMovieTypeByNameAsync(string name);
        Task<Genre> GetGenreByNameAsync(string name);
        Task<Country> GetCountryByNameAsync(string name);
    }
}
