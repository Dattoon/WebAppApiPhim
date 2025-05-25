using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MetadataService> _logger;

        public MetadataService(ApplicationDbContext context, ILogger<MetadataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AddGenreAsync(string name, string slug)
        {
            try
            {
                if (await GenreExistsAsync(name))
                {
                    return false; // Genre already exists
                }

                var genre = new MovieGenre
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Slug = slug
                };

                _context.MovieGenres.Add(genre);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding genre: {genreName}", name);
                return false;
            }
        }

        public async Task<bool> AddCountryAsync(string name, string code, string slug)
        {
            try
            {
                if (await CountryExistsAsync(name))
                {
                    return false; // Country already exists
                }

                var country = new MovieCountry
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Code = code,
                    Slug = slug
                };

                _context.MovieCountries.Add(country);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding country: {countryName}", name);
                return false;
            }
        }

        public async Task<bool> AddMovieTypeAsync(string name, string slug)
        {
            try
            {
                var existingType = await _context.MovieTypes
                    .FirstOrDefaultAsync(t => t.Name == name);

                if (existingType != null)
                {
                    return false; // Type already exists
                }

                var movieType = new MovieType
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Slug = slug
                };

                _context.MovieTypes.Add(movieType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie type: {typeName}", name);
                return false;
            }
        }

        public async Task<bool> GenreExistsAsync(string name)
        {
            try
            {
                return await _context.MovieGenres
                    .AnyAsync(g => g.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if genre exists: {genreName}", name);
                return false;
            }
        }

        public async Task<bool> CountryExistsAsync(string name)
        {
            try
            {
                return await _context.MovieCountries
                    .AnyAsync(c => c.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if country exists: {countryName}", name);
                return false;
            }
        }

        public Task UpdateMetadataAsync(MovieDetailResponse movie, string slug)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetMetadataAsync(string key)
        {
            throw new NotImplementedException();
        }
    }
}
