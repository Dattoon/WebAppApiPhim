using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
    public class MovieApiService : IMovieApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MovieApiService> _logger;
        private readonly TimeSpan _cacheTime = TimeSpan.FromHours(24);

        public MovieApiService(
            HttpClient httpClient,
            IMemoryCache memoryCache,
            ILogger<MovieApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<string>> GetGenresAsync()
        {
            string cacheKey = "genres";
            if (_memoryCache.TryGetValue(cacheKey, out List<string>? cachedGenres))
            {
                _logger.LogInformation("Cache hit for genres");
                return cachedGenres ?? new List<string>();
            }

            try
            {
                var response = await _httpClient.GetAsync("api/genres");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var genres = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();

                _memoryCache.Set(cacheKey, genres, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(genres),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation("Fetched and cached genres");
                return genres;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching genres");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetCountriesAsync()
        {
            string cacheKey = "countries";
            if (_memoryCache.TryGetValue(cacheKey, out List<string>? cachedCountries))
            {
                _logger.LogInformation("Cache hit for countries");
                return cachedCountries ?? new List<string>();
            }

            try
            {
                var response = await _httpClient.GetAsync("api/countries");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var countries = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();

                _memoryCache.Set(cacheKey, countries, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(countries),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation("Fetched and cached countries");
                return countries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching countries");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetMovieTypesAsync()
        {
            string cacheKey = "movie_types";
            if (_memoryCache.TryGetValue(cacheKey, out List<string>? cachedTypes))
            {
                _logger.LogInformation("Cache hit for movie types");
                return cachedTypes ?? new List<string>();
            }

            try
            {
                var response = await _httpClient.GetAsync("api/movie-types");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var types = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();

                _memoryCache.Set(cacheKey, types, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(types),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation("Fetched and cached movie types");
                return types;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching movie types");
                return new List<string>();
            }
        }

        public async Task<Models.MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string apiVersion = "v3")
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            string cacheKey = $"movie_detail_{slug}_{apiVersion}";
            if (_memoryCache.TryGetValue(cacheKey, out Models.MovieDetailResponse? cachedMovie))
            {
                _logger.LogInformation($"Cache hit for movie detail: {slug}");
                return cachedMovie ?? new Models.MovieDetailResponse();
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/movies/{slug}?v={apiVersion}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var movie = JsonSerializer.Deserialize<Models.MovieDetailResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Models.MovieDetailResponse();

                _memoryCache.Set(cacheKey, movie, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(movie),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation($"Fetched and cached movie detail for: {slug}");
                return movie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching movie detail for: {slug}");
                return new Models.MovieDetailResponse();
            }
        }

        public async Task<Models.MovieListResponse> GetMoviesByCategoryAsync(string category, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category is required", nameof(category));

            string cacheKey = $"movies_by_category_{category}_{page}";
            if (_memoryCache.TryGetValue(cacheKey, out Models.MovieListResponse? cachedResponse))
            {
                _logger.LogInformation($"Cache hit for movies by category: {category}, page: {page}");
                return cachedResponse ?? new Models.MovieListResponse();
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/movies/category/{category}?page={page}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var movies = JsonSerializer.Deserialize<Models.MovieListResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Models.MovieListResponse();

                _memoryCache.Set(cacheKey, movies, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(movies),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation($"Fetched and cached movies by category: {category}, page: {page}");
                return movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching movies by category: {category}, page: {page}");
                return new Models.MovieListResponse { Data = new List<Models.MovieItem>(), Pagination = new Models.Pagination() };
            }
        }

        public async Task<Models.MovieListResponse> GetMoviesByGenreAsync(string genreSlug, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(genreSlug))
                throw new ArgumentException("Genre slug is required", nameof(genreSlug));

            string cacheKey = $"movies_by_genre_{genreSlug}_{page}";
            if (_memoryCache.TryGetValue(cacheKey, out Models.MovieListResponse? cachedResponse))
            {
                _logger.LogInformation($"Cache hit for movies by genre: {genreSlug}, page: {page}");
                return cachedResponse ?? new Models.MovieListResponse();
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/movies/genre/{genreSlug}?page={page}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var movies = JsonSerializer.Deserialize<Models.MovieListResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Models.MovieListResponse();

                _memoryCache.Set(cacheKey, movies, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(movies),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation($"Fetched and cached movies by genre: {genreSlug}, page: {page}");
                return movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching movies by genre: {genreSlug}, page: {page}");
                return new Models.MovieListResponse { Data = new List<Models.MovieItem>(), Pagination = new Models.Pagination() };
            }
        }

        public async Task<Models.MovieListResponse> GetMoviesByCountryAsync(string countrySlug, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(countrySlug))
                throw new ArgumentException("Country slug is required", nameof(countrySlug));

            string cacheKey = $"movies_by_country_{countrySlug}_{page}";
            if (_memoryCache.TryGetValue(cacheKey, out Models.MovieListResponse? cachedResponse))
            {
                _logger.LogInformation($"Cache hit for movies by country: {countrySlug}, page: {page}");
                return cachedResponse ?? new Models.MovieListResponse();
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/movies/country/{countrySlug}?page={page}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var movies = JsonSerializer.Deserialize<Models.MovieListResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Models.MovieListResponse();

                _memoryCache.Set(cacheKey, movies, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(movies),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation($"Fetched and cached movies by country: {countrySlug}, page: {page}");
                return movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching movies by country: {countrySlug}, page: {page}");
                return new Models.MovieListResponse { Data = new List<Models.MovieItem>(), Pagination = new Models.Pagination() };
            }
        }

        public async Task<Models.MovieListResponse> GetMoviesByTypeAsync(string typeSlug, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(typeSlug))
                throw new ArgumentException("Type slug is required", nameof(typeSlug));

            string cacheKey = $"movies_by_type_{typeSlug}_{page}";
            if (_memoryCache.TryGetValue(cacheKey, out Models.MovieListResponse ? cachedResponse))
            {
                _logger.LogInformation($"Cache hit for movies by type: {typeSlug}, page: {page}");
                return cachedResponse ?? new Models.MovieListResponse();
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/movies/type/{typeSlug}?page={page}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var movies = JsonSerializer.Deserialize<Models.MovieListResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Models.MovieListResponse();

                _memoryCache.Set(cacheKey, movies, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(movies),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation($"Fetched and cached movies by type: {typeSlug}, page: {page}");
                return movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching movies by type: {typeSlug}, page: {page}");
                return new Models.MovieListResponse { Data = new List<Models.MovieItem>(), Pagination = new Models.Pagination() };
            }
        }

        public async Task<Models.ImageResponse> GetMovieImagesAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            string cacheKey = $"movie_images_{slug}";
            if (_memoryCache.TryGetValue(cacheKey, out Models.ImageResponse? cachedImages))
            {
                _logger.LogInformation($"Cache hit for images: {slug}");
                return cachedImages ?? new Models.ImageResponse();
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/movies/{slug}/images");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var images = JsonSerializer.Deserialize<Models.ImageResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Models.ImageResponse();

                _memoryCache.Set(cacheKey, images, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(images),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation($"Fetched and cached images for: {slug}");
                return images;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching images for: {slug}");
                return new Models.ImageResponse { Success = false };
            }
        }

        public async Task<ProductionApiResponse> GetProductionDataAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            string cacheKey = $"production_data_{slug}";
            if (_memoryCache.TryGetValue(cacheKey, out ProductionApiResponse? cachedData))
            {
                _logger.LogInformation($"Cache hit for production data: {slug}");
                return cachedData ?? new ProductionApiResponse();
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/movies/{slug}/production");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ProductionApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ProductionApiResponse();

                _memoryCache.Set(cacheKey, data, new MemoryCacheEntryOptions
                {
                    Size = EstimateCacheSize(data),
                    AbsoluteExpirationRelativeToNow = _cacheTime
                });
                _logger.LogInformation($"Fetched and cached production data for: {slug}");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching production data for: {slug}");
                return new ProductionApiResponse { Status = "error" };
            }
        }

        private int EstimateCacheSize(object data)
        {
            if (data == null) return 1024;

            try
            {
                var json = JsonSerializer.Serialize(data);
                return json.Length * sizeof(char);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error estimating cache size, using default size");
                return 1024;
            }
        }

        Task<Models.MovieDetailResponse> IMovieApiService.GetMovieDetailBySlugAsync(string slug, string apiVersion)
        {
            throw new NotImplementedException();
        }

        Task<Models.MovieListResponse> IMovieApiService.GetMoviesByCategoryAsync(string category, int page)
        {
            throw new NotImplementedException();
        }

        Task<Models.MovieListResponse> IMovieApiService.GetMoviesByGenreAsync(string genreSlug, int page)
        {
            throw new NotImplementedException();
        }

        Task<Models.MovieListResponse> IMovieApiService.GetMoviesByCountryAsync(string countrySlug, int page)
        {
            throw new NotImplementedException();
        }

        Task<Models.MovieListResponse> IMovieApiService.GetMoviesByTypeAsync(string typeSlug, int page)
        {
            throw new NotImplementedException();
        }

        Task<Models.ImageResponse> IMovieApiService.GetMovieImagesAsync(string slug)
        {
            throw new NotImplementedException();
        }

        public Task<Models.MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 20, string version = null)
        {
            throw new NotImplementedException();
        }
    }
}