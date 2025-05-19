using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Repositories
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly ApplicationDbContext _context;

        public MetadataRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<Country>> GetCountriesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Country> GetCountryByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<Genre> GetGenreByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<List<Genre>> GetGenresAsync()
        {
            throw new NotImplementedException();
        }

        public Task<MovieType> GetMovieTypeByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<List<MovieType>> GetMovieTypesAsync()
        {
            return await _context.MovieTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public Task<List<string>> GetYearsAsync(int startYear = 2000)
        {
            throw new NotImplementedException();
        }

        // Implement other methods...
    }
}
