using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MetadataService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTime = TimeSpan.FromHours(6);

        public MetadataService(
            ApplicationDbContext context,
            ILogger<MetadataService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        #region Simple String Lists (for dropdowns)

        public async Task<List<string>> GetGenresAsync()
        {
            const string cacheKey = "genres_simple";
            if (_cache.TryGetValue(cacheKey, out List<string> cachedGenres))
            {
                return cachedGenres;
            }

            try
            {
                var genres = await _context.MovieGenres
                    .OrderBy(g => g.Name)
                    .Select(g => g.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, genres, _cacheTime);
                return genres;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genres from database");
                // Fallback to hardcoded list
                return GetFallbackGenres();
            }
        }

        public async Task<List<string>> GetCountriesAsync()
        {
            const string cacheKey = "countries_simple";
            if (_cache.TryGetValue(cacheKey, out List<string> cachedCountries))
            {
                return cachedCountries;
            }

            try
            {
                var countries = await _context.MovieCountries
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, countries, _cacheTime);
                return countries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving countries from database");
                return GetFallbackCountries();
            }
        }

        public async Task<List<string>> GetMovieTypesAsync()
        {
            const string cacheKey = "types_simple";
            if (_cache.TryGetValue(cacheKey, out List<string> cachedTypes))
            {
                return cachedTypes;
            }

            try
            {
                var types = await _context.MovieTypes
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, types, _cacheTime);
                return types;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movie types from database");
                return GetFallbackMovieTypes();
            }
        }

        #endregion

        #region Full Entity Methods

        public async Task<List<MovieGenre>> GetAllGenresAsync()
        {
            const string cacheKey = "genres_full";
            if (_cache.TryGetValue(cacheKey, out List<MovieGenre> cachedGenres))
            {
                return cachedGenres;
            }

            try
            {
                var genres = await _context.MovieGenres
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, genres, _cacheTime);
                return genres;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all genres");
                return new List<MovieGenre>();
            }
        }

        public async Task<List<MovieCountry>> GetAllCountriesAsync()
        {
            const string cacheKey = "countries_full";
            if (_cache.TryGetValue(cacheKey, out List<MovieCountry> cachedCountries))
            {
                return cachedCountries;
            }

            try
            {
                var countries = await _context.MovieCountries
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, countries, _cacheTime);
                return countries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all countries");
                return new List<MovieCountry>();
            }
        }

        public async Task<List<MovieType>> GetAllMovieTypesAsync()
        {
            const string cacheKey = "types_full";
            if (_cache.TryGetValue(cacheKey, out List<MovieType> cachedTypes))
            {
                return cachedTypes;
            }

            try
            {
                var types = await _context.MovieTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, types, _cacheTime);
                return types;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all movie types");
                return new List<MovieType>();
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<bool> AddGenreAsync(string name, string slug, string description = null)
        {
            try
            {
                if (await GenreExistsAsync(name))
                {
                    return false; // Already exists
                }

                var genre = new MovieGenre
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Slug = slug ?? name.ToLower().Replace(" ", "-"),
                   
                };

                _context.MovieGenres.Add(genre);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("genres_simple");
                _cache.Remove("genres_full");

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
                    return false; // Already exists
                }

                var country = new MovieCountry
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Code = code ?? "XX",
                    Slug = slug ?? name.ToLower().Replace(" ", "-")
                };

                _context.MovieCountries.Add(country);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("countries_simple");
                _cache.Remove("countries_full");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding country: {countryName}", name);
                return false;
            }
        }

        public async Task<bool> AddMovieTypeAsync(string name, string slug, string description = null)
        {
            try
            {
                var existingType = await _context.MovieTypes
                    .FirstOrDefaultAsync(t => t.Name == name);

                if (existingType != null)
                {
                    return false; // Already exists
                }

                var movieType = new MovieType
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Slug = slug ?? name.ToLower().Replace(" ", "-"),
                   
                };

                _context.MovieTypes.Add(movieType);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("types_simple");
                _cache.Remove("types_full");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie type: {typeName}", name);
                return false;
            }
        }

        #endregion

        #region Existence Checks

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

        #endregion

        #region Legacy Methods (for compatibility)

        public Task UpdateMetadataAsync(MovieDetailResponse movie, string slug)
        {
            // Implementation for updating metadata from movie details
            // This can be implemented based on your specific needs
            return Task.CompletedTask;
        }

        public Task<string> GetMetadataAsync(string key)
        {
            // Implementation for getting specific metadata by key
            // This can be implemented based on your specific needs
            return Task.FromResult(string.Empty);
        }

        #endregion

        #region Fallback Data

        private List<string> GetFallbackGenres()
        {
            return new List<string>
            {
                "Hành Động", "Tình Cảm", "Hài Hước", "Cổ Trang", "Tâm Lý", "Hình Sự",
                "Chiến Tranh", "Thể Thao", "Võ Thuật", "Viễn Tưởng", "Phiêu Lưu", "Khoa Học",
                "Kinh Dị", "Âm Nhạc", "Thần Thoại", "Tài Liệu", "Gia Đình", "Học Đường"
            };
        }

        private List<string> GetFallbackCountries()
        {
            return new List<string>
            {
                "Việt Nam", "Trung Quốc", "Hàn Quốc", "Nhật Bản", "Thái Lan", "Âu Mỹ",
                "Ấn Độ", "Nga", "Philippines", "Hong Kong", "Đài Loan", "Khác"
            };
        }

        private List<string> GetFallbackMovieTypes()
        {
            return new List<string> { "Phim Lẻ", "Phim Bộ", "Hoạt Hình", "TV Shows" };
        }

        public Task<bool> AddGenreAsync(string name, string slug)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddMovieTypeAsync(string name, string slug)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
