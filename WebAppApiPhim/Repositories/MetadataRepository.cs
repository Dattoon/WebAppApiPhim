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
    public class MetadataRepository
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
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MovieGenre>> GetAllGenresAsync()
        {
            string cacheKey = "all_genres";
            if (_cache.TryGetValue(cacheKey, out List<MovieGenre> cachedGenres))
            {
                _logger.LogInformation("Cache hit for genres");
                return cachedGenres;
            }

            try
            {
                var genres = await _context.MovieGenres
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, genres, _cacheTime);
                _logger.LogInformation("Fetched and cached genres");
                return genres;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching genres");
                return new List<MovieGenre>();
            }
        }

        public async Task<List<MovieCountry>> GetAllCountriesAsync()
        {
            string cacheKey = "all_countries";
            if (_cache.TryGetValue(cacheKey, out List<MovieCountry> cachedCountries))
            {
                _logger.LogInformation("Cache hit for countries");
                return cachedCountries;
            }

            try
            {
                var countries = await _context.MovieCountries
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, countries, _cacheTime);
                _logger.LogInformation("Fetched and cached countries");
                return countries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching countries");
                return new List<MovieCountry>();
            }
        }

        public async Task<List<MovieType>> GetAllMovieTypesAsync()
        {
            string cacheKey = "all_movie_types";
            if (_cache.TryGetValue(cacheKey, out List<MovieType> cachedTypes))
            {
                _logger.LogInformation("Cache hit for movie types");
                return cachedTypes;
            }

            try
            {
                var types = await _context.MovieTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, types, _cacheTime);
                _logger.LogInformation("Fetched and cached movie types");
                return types;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching movie types");
                return new List<MovieType>();
            }
        }

        public async Task<MovieGenre> GetGenreByIdAsync(string id)
        {
            string cacheKey = $"genre_{id}";
            if (_cache.TryGetValue(cacheKey, out MovieGenre cachedGenre))
            {
                _logger.LogInformation($"Cache hit for genre {id}");
                return cachedGenre;
            }

            try
            {
                var genre = await _context.MovieGenres
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (genre != null)
                {
                    _cache.Set(cacheKey, genre, _cacheTime);
                    _logger.LogInformation($"Fetched and cached genre {id}");
                }
                else
                {
                    _logger.LogWarning($"Genre {id} not found");
                }

                return genre;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching genre {id}");
                return null;
            }
        }

        public async Task<MovieCountry> GetCountryByIdAsync(string id)
        {
            string cacheKey = $"country_{id}";
            if (_cache.TryGetValue(cacheKey, out MovieCountry cachedCountry))
            {
                _logger.LogInformation($"Cache hit for country {id}");
                return cachedCountry;
            }

            try
            {
                var country = await _context.MovieCountries
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (country != null)
                {
                    _cache.Set(cacheKey, country, _cacheTime);
                    _logger.LogInformation($"Fetched and cached country {id}");
                }
                else
                {
                    _logger.LogWarning($"Country {id} not found");
                }

                return country;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching country {id}");
                return null;
            }
        }

        public async Task<MovieType> GetMovieTypeByIdAsync(string id)
        {
            string cacheKey = $"movie_type_{id}";
            if (_cache.TryGetValue(cacheKey, out MovieType cachedType))
            {
                _logger.LogInformation($"Cache hit for movie type {id}");
                return cachedType;
            }

            try
            {
                var type = await _context.MovieTypes
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (type != null)
                {
                    _cache.Set(cacheKey, type, _cacheTime);
                    _logger.LogInformation($"Fetched and cached movie type {id}");
                }
                else
                {
                    _logger.LogWarning($"Movie type {id} not found");
                }

                return type;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching movie type {id}");
                return null;
            }
        }

        private int EstimateCacheSize(object data)
        {
            if (data == null)
                return 1024;

            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(data);
                return json.Length * sizeof(char);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error estimating cache size, using default size");
                return 1024;
            }
        }
    }
}