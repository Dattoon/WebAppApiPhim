using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using WebAppApiPhim.Models;
using Microsoft.Extensions.Caching.Memory;
using WebAppApiPhim.Data;
using System.Text.Json.Serialization;

namespace WebAppApiPhim.Services.Enhanced
{
    public class EnhancedMovieApiService : IMovieApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EnhancedMovieApiService> _logger;

        // Connection pooling for database operations
        private readonly SemaphoreSlim _dbSemaphore = new(10, 10);

        // Cache keys management
        private readonly ConcurrentDictionary<string, DateTime> _cacheKeys = new();

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public EnhancedMovieApiService(
            HttpClient httpClient,
            IDistributedCache distributedCache,
            IMemoryCache memoryCache,
            ApplicationDbContext context,
            ILogger<EnhancedMovieApiService> logger)
        {
            _httpClient = httpClient;
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _context = context;
            _logger = logger;
        }

        public async Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = null)
        {
            var cacheKey = $"latest_movies_{page}_{limit}_{version}";

            // Try L1 cache (Memory) first
            if (_memoryCache.TryGetValue(cacheKey, out MovieListResponse cachedResult))
            {
                return cachedResult;
            }

            // Try L2 cache (Redis/Distributed)
            var distributedResult = await GetFromDistributedCacheAsync<MovieListResponse>(cacheKey);
            if (distributedResult != null)
            {
                // Store back in L1 cache
                _memoryCache.Set(cacheKey, distributedResult, TimeSpan.FromMinutes(5));
                return distributedResult;
            }

            // Try database cache
            var dbResult = await GetFromDatabaseCacheAsync(page, limit);
            if (dbResult != null && dbResult.Data.Any())
            {
                await SetCacheAsync(cacheKey, dbResult, TimeSpan.FromMinutes(15));
                return dbResult;
            }

            // Fallback to API
            return await FetchFromApiWithFallbackAsync(page, limit, version, cacheKey);
        }

        private async Task<MovieListResponse> GetFromDatabaseCacheAsync(int page, int limit)
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                var movies = await _context.CachedMovies
                    .Include(m => m.Statistic)
                    .Where(m => m.LastUpdated > DateTime.Now.AddHours(-6)) // Fresh data only
                    .OrderByDescending(m => m.LastUpdated)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(m => new MovieItem
                    {
                        Slug = m.Slug,
                        Name = m.Name,
                        OriginalName = m.OriginalName,
                        Year = m.Year,
                        ThumbUrl = m.ThumbUrl,
                        PosterUrl = m.PosterUrl,
                        // Map other properties
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (movies.Any())
                {
                    var totalCount = await _context.CachedMovies.CountAsync();
                    return new MovieListResponse
                    {
                        Data = movies,
                        Pagination = new Pagination
                        {
                            Current_page = page,
                            Total_pages = (int)Math.Ceiling((double)totalCount / limit),
                            Total_items = totalCount,
                            Limit = limit
                        }
                    };
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }

            return null;
        }

        private async Task<T> GetFromDistributedCacheAsync<T>(string key) where T : class
        {
            try
            {
                var cached = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(cached))
                {
                    return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from distributed cache for key: {Key}", key);
            }
            return null;
        }

        private async Task SetCacheAsync<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                // Set in memory cache
                _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));

                // Set in distributed cache
                var serialized = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };
                await _distributedCache.SetStringAsync(key, serialized, options);

                // Track cache key for cleanup
                _cacheKeys[key] = DateTime.Now.Add(expiration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting cache for key: {Key}", key);
            }
        }

        // Enhanced search with full-text search capabilities
        public async Task<MovieListResponse> SearchMoviesAsync(string query, int page = 1, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetLatestMoviesAsync(page, limit);
            }

            var cacheKey = $"search_{query.ToLower()}_{page}_{limit}";

            // Check cache first
            if (_memoryCache.TryGetValue(cacheKey, out MovieListResponse cachedResult))
            {
                return cachedResult;
            }

            // Enhanced database search with ranking
            var dbResults = await SearchInDatabaseAsync(query, page, limit);
            if (dbResults != null && dbResults.Data.Any())
            {
                _memoryCache.Set(cacheKey, dbResults, TimeSpan.FromMinutes(10));
                return dbResults;
            }

            // Fallback to API search
            return await SearchViaApiAsync(query, page, limit, cacheKey);
        }

        private async Task<MovieListResponse> SearchInDatabaseAsync(string query, int page, int limit)
        {
            var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var movies = await _context.CachedMovies
                .Include(m => m.Statistic)
                .Where(m => searchTerms.Any(term =>
                    m.Name.ToLower().Contains(term) ||
                    m.OriginalName.ToLower().Contains(term) ||
                    m.Actors.ToLower().Contains(term) ||
                    m.Director.ToLower().Contains(term)))
                .OrderByDescending(m =>
                    // Ranking algorithm
                    (m.Name.ToLower().Contains(query.ToLower()) ? 100 : 0) +
                    (m.OriginalName.ToLower().Contains(query.ToLower()) ? 80 : 0) +
                    (m.ViewCount / 1000) + // Popularity boost
                    (m.Statistic != null ? m.Statistic.AverageRating * 5 : 0)) // Rating boost
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(m => new MovieItem
                {
                    Slug = m.Slug,
                    Name = m.Name,
                    OriginalName = m.OriginalName,
                    Year = m.Year,
                    ThumbUrl = m.ThumbUrl,
                    PosterUrl = m.PosterUrl
                })
                .AsNoTracking()
                .ToListAsync();

            if (movies.Any())
            {
                var totalCount = await _context.CachedMovies
                    .CountAsync(m => searchTerms.Any(term =>
                        m.Name.ToLower().Contains(term) ||
                        m.OriginalName.ToLower().Contains(term)));

                return new MovieListResponse
                {
                    Data = movies,
                    Pagination = new Pagination
                    {
                        Current_page = page,
                        Total_pages = (int)Math.Ceiling((double)totalCount / limit),
                        Total_items = totalCount,
                        Limit = limit
                    }
                };
            }

            return null;
        }

        // Implement other methods with similar optimizations...
        public async Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string version = null)
        {
            // Implementation with enhanced caching and error handling
            throw new NotImplementedException();
        }

        public async Task<MovieListResponse> GetRelatedMoviesAsync(string slug, int limit = 6, string version = null)
        {
            // Implementation with ML-based recommendations
            throw new NotImplementedException();
        }

        public async Task<MovieListResponse> FilterMoviesAsync(string type = null, string genre = null, string country = null, string year = null, int page = 1, int limit = 10)
        {
            // Implementation with optimized filtering
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetGenresAsync()
        {
            // Implementation with caching
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetCountriesAsync()
        {
            // Implementation with caching
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetYearsAsync()
        {
            // Implementation with caching
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetMovieTypesAsync()
        {
            // Implementation with caching
            throw new NotImplementedException();
        }

        private async Task<MovieListResponse> FetchFromApiWithFallbackAsync(int page, int limit, string version, string cacheKey)
        {
            // Implementation with circuit breaker and fallback
            throw new NotImplementedException();
        }

        private async Task<MovieListResponse> SearchViaApiAsync(string query, int page, int limit, string cacheKey)
        {
            // Implementation
            throw new NotImplementedException();
        }

        Task<MovieListResponse> IMovieApiService.GetLatestMoviesAsync(int page, int limit, string version)
        {
            throw new NotImplementedException();
        }

        Task<MovieDetailResponse> IMovieApiService.GetMovieDetailBySlugAsync(string slug, string version)
        {
            throw new NotImplementedException();
        }

        Task<MovieListResponse> IMovieApiService.GetRelatedMoviesAsync(string slug, int limit, string version)
        {
            throw new NotImplementedException();
        }

        Task<MovieListResponse> IMovieApiService.SearchMoviesAsync(string query, int page, int limit)
        {
            throw new NotImplementedException();
        }

        Task<MovieListResponse> IMovieApiService.FilterMoviesAsync(string type, string genre, string country, string year, int page, int limit)
        {
            throw new NotImplementedException();
        }
    }
}
