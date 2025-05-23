using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Repositories
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MetadataRepository> _logger;
        private readonly TimeSpan _cacheTime = TimeSpan.FromHours(24);

        public MetadataRepository(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<MetadataRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        #region Genre Methods
        public async Task<List<Genre>> GetAllGenresAsync()
        {
            string cacheKey = "all_genres_list";

            if (_cache.TryGetValue(cacheKey, out List<Genre> cachedGenres))
            {
                return cachedGenres;
            }

            var genres = await _context.Genres
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();

            _cache.Set(cacheKey, genres, _cacheTime);

            return genres;
        }

        public async Task<Genre> GetGenreByIdAsync(int id)
        {
            string cacheKey = $"genre_{id}";

            if (_cache.TryGetValue(cacheKey, out Genre cachedGenre))
            {
                return cachedGenre;
            }

            var genre = await _context.Genres.FindAsync(id);

            if (genre != null)
            {
                _cache.Set(cacheKey, genre, _cacheTime);
            }

            return genre;
        }

        public async Task<Genre> GetGenreBySlugAsync(string slug)
        {
            string cacheKey = $"genre_slug_{slug}";

            if (_cache.TryGetValue(cacheKey, out Genre cachedGenre))
            {
                return cachedGenre;
            }

            var genre = await _context.Genres
                .FirstOrDefaultAsync(g => g.Slug == slug && g.IsActive);

            if (genre != null)
            {
                _cache.Set(cacheKey, genre, _cacheTime);
            }

            return genre;
        }

        public async Task<Genre> AddGenreAsync(Genre genre)
        {
            try
            {
                _context.Genres.Add(genre);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_genres_list");

                return genre;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding genre: {genre.Name}");
                throw;
            }
        }

        public async Task<Genre> UpdateGenreAsync(Genre genre)
        {
            try
            {
                _context.Genres.Update(genre);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_genres_list");
                _cache.Remove($"genre_{genre.Id}");
                _cache.Remove($"genre_slug_{genre.Slug}");

                return genre;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating genre: {genre.Name}");
                throw;
            }
        }

        public async Task<bool> DeleteGenreAsync(int id)
        {
            try
            {
                var genre = await _context.Genres.FindAsync(id);

                if (genre == null)
                    return false;

                // Soft delete
                genre.IsActive = false;
                genre.UpdatedAt = DateTime.Now;

                _context.Genres.Update(genre);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_genres_list");
                _cache.Remove($"genre_{id}");
                _cache.Remove($"genre_slug_{genre.Slug}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting genre with ID: {id}");
                return false;
            }
        }
        #endregion

        #region Country Methods
        public async Task<List<Country>> GetAllCountriesAsync()
        {
            string cacheKey = "all_countries_list";

            if (_cache.TryGetValue(cacheKey, out List<Country> cachedCountries))
            {
                return cachedCountries;
            }

            var countries = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            _cache.Set(cacheKey, countries, _cacheTime);

            return countries;
        }

        public async Task<Country> GetCountryByIdAsync(int id)
        {
            string cacheKey = $"country_{id}";

            if (_cache.TryGetValue(cacheKey, out Country cachedCountry))
            {
                return cachedCountry;
            }

            var country = await _context.Countries.FindAsync(id);

            if (country != null)
            {
                _cache.Set(cacheKey, country, _cacheTime);
            }

            return country;
        }

        public async Task<Country> GetCountryByCodeAsync(string code)
        {
            string cacheKey = $"country_code_{code}";

            if (_cache.TryGetValue(cacheKey, out Country cachedCountry))
            {
                return cachedCountry;
            }

            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);

            if (country != null)
            {
                _cache.Set(cacheKey, country, _cacheTime);
            }

            return country;
        }

        public async Task<Country> AddCountryAsync(Country country)
        {
            try
            {
                _context.Countries.Add(country);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_countries_list");

                return country;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding country: {country.Name}");
                throw;
            }
        }

        public async Task<Country> UpdateCountryAsync(Country country)
        {
            try
            {
                _context.Countries.Update(country);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_countries_list");
                _cache.Remove($"country_{country.Id}");
                _cache.Remove($"country_code_{country.Code}");

                return country;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating country: {country.Name}");
                throw;
            }
        }

        public async Task<bool> DeleteCountryAsync(int id)
        {
            try
            {
                var country = await _context.Countries.FindAsync(id);

                if (country == null)
                    return false;

                // Soft delete
                country.IsActive = false;
                country.UpdatedAt = DateTime.Now;

                _context.Countries.Update(country);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_countries_list");
                _cache.Remove($"country_{id}");
                _cache.Remove($"country_code_{country.Code}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting country with ID: {id}");
                return false;
            }
        }
        #endregion

        #region MovieType Methods
        public async Task<List<MovieType>> GetAllMovieTypesAsync()
        {
            string cacheKey = "all_movie_types_list";

            if (_cache.TryGetValue(cacheKey, out List<MovieType> cachedMovieTypes))
            {
                return cachedMovieTypes;
            }

            var movieTypes = await _context.MovieTypes
                .Where(mt => mt.IsActive)
                .OrderBy(mt => mt.Name)
                .ToListAsync();

            _cache.Set(cacheKey, movieTypes, _cacheTime);

            return movieTypes;
        }

        public async Task<MovieType> GetMovieTypeByIdAsync(int id)
        {
            string cacheKey = $"movie_type_{id}";

            if (_cache.TryGetValue(cacheKey, out MovieType cachedMovieType))
            {
                return cachedMovieType;
            }

            var movieType = await _context.MovieTypes.FindAsync(id);

            if (movieType != null)
            {
                _cache.Set(cacheKey, movieType, _cacheTime);
            }

            return movieType;
        }

        public async Task<MovieType> GetMovieTypeBySlugAsync(string slug)
        {
            string cacheKey = $"movie_type_slug_{slug}";

            if (_cache.TryGetValue(cacheKey, out MovieType cachedMovieType))
            {
                return cachedMovieType;
            }

            var movieType = await _context.MovieTypes
                .FirstOrDefaultAsync(mt => mt.Slug == slug && mt.IsActive);

            if (movieType != null)
            {
                _cache.Set(cacheKey, movieType, _cacheTime);
            }

            return movieType;
        }

        public async Task<MovieType> AddMovieTypeAsync(MovieType movieType)
        {
            try
            {
                _context.MovieTypes.Add(movieType);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_movie_types_list");

                return movieType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding movie type: {movieType.Name}");
                throw;
            }
        }

        public async Task<MovieType> UpdateMovieTypeAsync(MovieType movieType)
        {
            try
            {
                _context.MovieTypes.Update(movieType);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_movie_types_list");
                _cache.Remove($"movie_type_{movieType.Id}");
                _cache.Remove($"movie_type_slug_{movieType.Slug}");

                return movieType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating movie type: {movieType.Name}");
                throw;
            }
        }

        public async Task<bool> DeleteMovieTypeAsync(int id)
        {
            try
            {
                var movieType = await _context.MovieTypes.FindAsync(id);

                if (movieType == null)
                    return false;

                // Soft delete
                movieType.IsActive = false;
                movieType.UpdatedAt = DateTime.Now;

                _context.MovieTypes.Update(movieType);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_movie_types_list");
                _cache.Remove($"movie_type_{id}");
                _cache.Remove($"movie_type_slug_{movieType.Slug}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting movie type with ID: {id}");
                return false;
            }
        }
        #endregion

        public async Task UpdateMovieCountsAsync()
        {
            try
            {
                // Update genre movie counts
                var genres = await _context.Genres.Where(g => g.IsActive).ToListAsync();
                foreach (var genre in genres)
                {
                    var count = await _context.CachedMovies
                        .Where(m => m.Genres.Contains(genre.Name))
                        .CountAsync();

                    genre.MovieCount = count;
                    genre.UpdatedAt = DateTime.Now;
                }

                // Update country movie counts
                var countries = await _context.Countries.Where(c => c.IsActive).ToListAsync();
                foreach (var country in countries)
                {
                    var count = await _context.CachedMovies
                        .Where(m => m.Country.Contains(country.Name))
                        .CountAsync();

                    country.MovieCount = count;
                    country.UpdatedAt = DateTime.Now;
                }

                // Update movie type counts
                var movieTypes = await _context.MovieTypes.Where(mt => mt.IsActive).ToListAsync();
                foreach (var movieType in movieTypes)
                {
                    var count = await _context.CachedMovies
                        .Where(m => m.Type.Contains(movieType.Name))
                        .CountAsync();

                    movieType.MovieCount = count;
                    movieType.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_genres_list");
                _cache.Remove("all_countries_list");
                _cache.Remove("all_movie_types_list");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie counts");
                throw;
            }
        }
    }
}
