using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Models;
using WebAppApiPhim.Data;
using System.Threading;
using WebAppApiPhim.Services.Helpers;
using Microsoft.Extensions.DependencyInjection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAppApiPhim.Services
{
    public class MovieApiService : IMovieApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MovieApiService> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _baseUrl = "https://api.dulieuphim.ink";
        private readonly string[] _apiVersions = new[] { "v1", "v3", "v2" };
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5); // Limit concurrent requests

        // Cache expiration times
        private readonly TimeSpan _shortCacheTime = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _mediumCacheTime = TimeSpan.FromHours(1);
        private readonly TimeSpan _longCacheTime = TimeSpan.FromDays(1);

        public MovieApiService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<MovieApiService> logger,
            ApplicationDbContext dbContext,
            IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _dbContext = dbContext;
            _scopeFactory = scopeFactory;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = null)
        {
            string cacheKey = $"latest_movies_{page}_{limit}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            // Determine which API versions to try
            var versionsToTry = version != null ? new[] { version } : _apiVersions;

            // Try each API version until one succeeds
            foreach (var ver in versionsToTry)
            {
                try
                {
                    string url = $"{_baseUrl}/phim-moi/{ver}?page={page}&limit={limit}";
                    _logger.LogInformation($"Fetching latest movies from {url}");

                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                    // Enrich movies with images in parallel
                    if (result?.Data != null && result.Data.Any())
                    {
                        await EnrichMoviesWithImagesAsync(result.Data);
                    }

                    // Cache the result
                    _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
     .SetSize(1)
     .SetAbsoluteExpiration(_shortCacheTime));


                    _logger.LogInformation($"Successfully fetched latest movies using {ver}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error fetching latest movies with {ver}: {ex.Message}");
                }
            }

            _logger.LogError("All API versions failed for GetLatestMoviesAsync");
            return new MovieListResponse
            {
                Data = new List<MovieItem>(),
                Pagination = new Pagination
                {
                    Current_page = page,
                    Total_pages = 1,
                    Total_items = 0,
                    Limit = limit
                }
            };
        }

        public async Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string version = null)
        {
            string cacheKey = $"movie_detail_{slug}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out MovieDetailResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            // Try to get from database
            var cachedMovie = await _dbContext.CachedMovies
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (cachedMovie != null && !string.IsNullOrEmpty(cachedMovie.RawData))
            {
                try
                {
                    var movieDetail = JsonSerializer.Deserialize<MovieDetailResponse>(cachedMovie.RawData, _jsonOptions);
                    if (movieDetail != null)
                    {
                        _cache.Set(cacheKey, movieDetail, new MemoryCacheEntryOptions()
     .SetSize(1)
     .SetAbsoluteExpiration(_mediumCacheTime));

                        return movieDetail;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error deserializing cached movie data for {slug}");
                }
            }

            // Determine which API versions to try
            var versionsToTry = version != null ? new[] { version } : _apiVersions;

            // Try each API version until one succeeds
            foreach (var ver in versionsToTry)
            {
                try
                {
                    string url = $"{_baseUrl}/phim-chi-tiet/{ver}?slug={slug}";
                    _logger.LogInformation($"Fetching movie detail from {url}");

                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MovieDetailResponse>(content, _jsonOptions);

                    if (result != null)
                    {
                        // Cache the result
                        _cache.Set(cacheKey, new ImageResponse
                        {
                            Success = true,
                            SubThumb = cachedMovie.ThumbUrl ?? "/placeholder.jpg",
                            SubPoster = cachedMovie.PosterUrl ?? "/placeholder.jpg",
                        }, new MemoryCacheEntryOptions()
    .SetSize(1)
    .SetAbsoluteExpiration(_longCacheTime));


                        // Store in database asynchronously
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await MovieDatabaseHelper.StoreMovieInDatabaseAsync(result, _dbContext, _logger, _jsonOptions);

                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error storing movie {slug} in database");
                            }
                        });

                        _logger.LogInformation($"Successfully fetched movie detail using {ver}");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error fetching movie detail with {ver}: {ex.Message}");
                }
            }

            _logger.LogError($"All API versions failed for GetMovieDetailBySlugAsync with slug: {slug}");
            return null;
        }

        public async Task<MovieListResponse> SearchMoviesAsync(string query, int page = 1, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetLatestMoviesAsync(page, limit);
            }
            string cacheKey = $"search_movies_{query}_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
                string url = $"{_baseUrl}/phim-data/v1?name={Uri.EscapeDataString(query)}&page={page}&limit={limit}";
                _logger.LogInformation($"Searching movies from {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Enrich with images
                if (result?.Data != null && result.Data.Any())
                {
                    await EnrichMoviesWithImagesAsync(result.Data);
                }

                // Cache the result (shorter time for search results)
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
    .SetSize(1)
    .SetAbsoluteExpiration(_shortCacheTime));


                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching movies: {ex.Message}");
                return new MovieListResponse
                {
                    Data = new List<MovieItem>(),
                    Pagination = new Pagination
                    {
                        Current_page = page,
                        Total_pages = 1,
                        Total_items = 0,
                        Limit = limit
                    }
                };
            }
        }

        public async Task<MovieListResponse> FilterMoviesAsync(
            string type = null,
            string genre = null,
            string country = null,
            string year = null,
            int page = 1,
            int limit = 10)
        {
            // Build cache key based on filter parameters
            string cacheKey = $"filter_movies_{type}_{genre}_{country}_{year}_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
                // Build URL with filter parameters
                string url = $"{_baseUrl}/phim-data/v1?page={page}&limit={limit}";

                if (!string.IsNullOrEmpty(type))
                    url += $"&loai_phim={Uri.EscapeDataString(type)}";

                if (!string.IsNullOrEmpty(genre))
                    url += $"&the_loai={Uri.EscapeDataString(genre)}";

                if (!string.IsNullOrEmpty(country))
                    url += $"&quoc_gia={Uri.EscapeDataString(country)}";

                if (!string.IsNullOrEmpty(year))
                    url += $"&year={Uri.EscapeDataString(year)}";

                _logger.LogInformation($"Filtering movies from {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Enrich with images
                if (result?.Data != null && result.Data.Any())
                {
                    await EnrichMoviesWithImagesAsync(result.Data);
                }

                // Cache the result
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
       .SetSize(1)
       .SetAbsoluteExpiration(_shortCacheTime));


                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error filtering movies: {ex.Message}");
                return new MovieListResponse
                {
                    Data = new List<MovieItem>(),
                    Pagination = new Pagination
                    {
                        Current_page = page,
                        Total_pages = 1,
                        Total_items = 0,
                        Limit = limit
                    }
                };
            }
        }

        public async Task<MovieListResponse> GetRelatedMoviesAsync(string slug, int limit = 6, string version = null)
        {
            string cacheKey = $"related_movies_{slug}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
                // Get movie details to extract genre/type for better recommendations
                var movieDetail = await GetMovieDetailBySlugAsync(slug);

                // If we have movie details, try to get movies with similar genres
                if (movieDetail != null && !string.IsNullOrEmpty(movieDetail.Genres))
                {
                    var genres = movieDetail.Genres.Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(genres))
                    {
                        var relatedByGenre = await FilterMoviesAsync(
                            type: movieDetail.Format,
                            genre: genres,
                            limit: limit);

                        if (relatedByGenre?.Data != null && relatedByGenre.Data.Any())
                        {
                            // Remove the current movie from results if present
                            relatedByGenre.Data = relatedByGenre.Data
                                .Where(m => m.Slug != slug)
                                .Take(limit)
                                .ToList();

                            if (relatedByGenre.Data.Any())
                            {
                                _cache.Set(cacheKey, relatedByGenre, new MemoryCacheEntryOptions()
    .SetSize(1)
    .SetAbsoluteExpiration(_shortCacheTime));

                                return relatedByGenre;
                            }
                        }
                    }
                }

                // Fallback to latest movies if we couldn't get related by genre
                var result = await GetLatestMoviesAsync(1, limit + 1, version);

                // Remove the current movie from results if present
                if (result?.Data != null)
                {
                    result.Data = result.Data
                        .Where(m => m.Slug != slug)
                        .Take(limit)
                        .ToList();
                }

                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
    .SetSize(1)
    .SetAbsoluteExpiration(_shortCacheTime));


                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting related movies for {slug}");
                return await GetLatestMoviesAsync(1, limit, version);
            }
        }

        // Metadata methods
        public async Task<List<string>> GetGenresAsync()
        {
            string cacheKey = "all_genres";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedGenres))
            {
                return cachedGenres;
            }

            // Get genres from database
            var genres = await _dbContext.Genres
                .Where(g => g.IsActive)
                .Select(g => g.Name)
                .ToListAsync();

            if (!genres.Any())
            {
                // Fallback to common genres
                genres = new List<string>
                {
                    "Hành Động", "Tình Cảm", "Hài Hước", "Cổ Trang", "Kinh Dị", "Tâm Lý",
                    "Khoa Học Viễn Tưởng", "Phiêu Lưu", "Hình Sự", "Chiến Tranh"
                };
            }

            _cache.Set(cacheKey, genres, new MemoryCacheEntryOptions()
      .SetSize(1)
      .SetAbsoluteExpiration(_longCacheTime));

            return genres;
        }

        public async Task<List<string>> GetCountriesAsync()
        {
            string cacheKey = "all_countries";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedCountries))
            {
                return cachedCountries;
            }

            // Get countries from database
            var countries = await _dbContext.Countries
                .Where(c => c.IsActive)
                .Select(c => c.Name)
                .ToListAsync();

            if (!countries.Any())
            {
                // Fallback to common countries
                countries = new List<string>
                {
                    "Việt Nam", "Trung Quốc", "Hàn Quốc", "Nhật Bản", "Thái Lan", "Âu Mỹ",
                    "Đài Loan", "Hồng Kông", "Ấn Độ", "Philippines", "Quốc gia khác"
                };
            }

            _cache.Set(cacheKey, countries, new MemoryCacheEntryOptions()
     .SetSize(1)
     .SetAbsoluteExpiration(_longCacheTime));

            return countries;
        }

        public async Task<List<string>> GetYearsAsync()
        {
            string cacheKey = "all_years";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedYears))
            {
                return cachedYears;
            }

            // Create list of years from 2000 to current year
            var years = new List<string>();
            int currentYear = DateTime.Now.Year;

            for (int year = currentYear; year >= 2000; year--)
            {
                years.Add(year.ToString());
            }

            _cache.Set(cacheKey, years, new MemoryCacheEntryOptions()
     .SetSize(1)
     .SetAbsoluteExpiration(_longCacheTime));

            return years;
        }

        public async Task<List<string>> GetMovieTypesAsync()
        {
            string cacheKey = "all_movie_types";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedTypes))
            {
                return cachedTypes;
            }

            // Get movie types from database
            var types = await _dbContext.MovieTypes
                .Where(t => t.IsActive)
                .Select(t => t.Name)
                .ToListAsync();

            if (!types.Any())
            {
                // Fallback to common types
                types = new List<string>
                {
                    "Phim lẻ", "Phim bộ", "Phim chiếu rạp", "Phim đang chiếu", "TV shows", "Hoạt hình"
                };
            }

            _cache.Set(cacheKey, types, new MemoryCacheEntryOptions()
     .SetSize(1)
     .SetAbsoluteExpiration(_longCacheTime));

            return types;
        }

        // Private helper methods sẽ được gửi trong phần tiếp theo
        private async Task EnrichMoviesWithImagesAsync(List<MovieItem> movies)
        {
            // Process in batches to avoid overwhelming the API
            const int batchSize = 5;

            for (int i = 0; i < movies.Count; i += batchSize)
            {
                var batch = movies.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(movie => EnrichMovieWithImageAsync(movie)).ToList();

                // Wait for all tasks in the batch to complete or timeout after 5 seconds
                await Task.WhenAny(
                    Task.WhenAll(tasks),
                    Task.Delay(5000)
                );
            }
        }

        private async Task EnrichMovieWithImageAsync(MovieItem movie)
        {
            string cacheKey = $"movie_images_{movie.Slug}";

            try
            {
                // 1. Lấy từ MemoryCache nếu có
                if (_cache.TryGetValue(cacheKey, out ImageResponse cachedImage) && cachedImage?.Success == true)
                {
                    movie.ThumbUrl = cachedImage.SubThumb;
                    movie.PosterUrl = cachedImage.SubPoster;
                    return;
                }

                // 2. Lấy từ DB nếu đã từng lưu
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cachedMovie = await scopedDbContext.CachedMovies.FirstOrDefaultAsync(m => m.Slug == movie.Slug);
                    if (cachedMovie != null && !string.IsNullOrEmpty(cachedMovie.ThumbUrl))
                    {
                        movie.ThumbUrl = cachedMovie.ThumbUrl;
                        movie.PosterUrl = cachedMovie.PosterUrl;

                        _cache.Set(cacheKey, new ImageResponse
                        {
                            Success = true,
                            SubThumb = cachedMovie.ThumbUrl,
                            SubPoster = cachedMovie.PosterUrl
                        }, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _longCacheTime,
                            Size = 1
                        });

                        return;
                    }
                }

                // 3. Gọi API get-img để lấy ảnh nếu cache và DB đều không có
                string[] imageApiVersions = { "v2", "v3", "v1" };
                foreach (var ver in imageApiVersions)
                {
                    try
                    {
                        string imageUrl = $"{_baseUrl}/get-img/{ver}?slug={movie.Slug}";
                        var response = await _httpClient.GetAsync(imageUrl);
                        response.EnsureSuccessStatusCode();

                        var content = await response.Content.ReadAsStringAsync();
                        var imgResult = JsonSerializer.Deserialize<ImageResponse>(content, _jsonOptions);

                        _logger.LogInformation($"Fetched image for {movie.Slug} from v{ver}: {JsonSerializer.Serialize(imgResult)}");

                        if (imgResult != null && imgResult.Success)
                        {
                            movie.ThumbUrl = imgResult.SubThumb;
                            movie.PosterUrl = imgResult.SubPoster;

                            _cache.Set(cacheKey, imgResult, new MemoryCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = _longCacheTime,
                                Size = 1
                            });

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Image fetch failed for {movie.Slug} (version {ver})");
                    }
                }

                // 4. Nếu tất cả đều thất bại → fallback ảnh mặc định
                movie.ThumbUrl = "/placeholder.svg?height=450&width=300";
                movie.PosterUrl = "/placeholder.svg?height=450&width=300";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enriching movie {movie.Slug} with images");
                movie.ThumbUrl = "/placeholder.svg?height=450&width=300";
                movie.PosterUrl = "/placeholder.svg?height=450&width=300";
            }
        }


        private void SaveMovieToDatabaseAsync(MovieDetailResponse movie)
        {
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<MovieApiService>>();

                try
                {
                    await MovieDatabaseHelper.StoreMovieInDatabaseAsync(movie, scopedDbContext, logger, _jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error storing movie {movie.Slug} in database");
                }
            });
        }
        private async Task StoreMovieInDatabaseAsync(MovieDetailResponse movie)
        {
            // Implementation sẽ được gửi trong phần tiếp theo để tránh quá dài
            // Tạm thời để trống, sẽ implement trong service tiếp theo
        }
    }
}
