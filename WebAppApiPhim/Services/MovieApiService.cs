using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public class MovieApiService : IMovieApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MovieApiService> _logger;
        private readonly string _baseUrl = "https://api.dulieuphim.ink";
        private readonly int _cacheExpirationMinutes = 15;
        private readonly string[] _apiVersions = new[] { "v1", "v3", "v2" };    
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5);    

        public MovieApiService(HttpClient httpClient, IMemoryCache cache, ILogger<MovieApiService> logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(120); 
            _cache = cache;
            _logger = logger;

            // Cấu hình JsonSerializer
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = null)
        {
            string cacheKey = $"latest_movies_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            // Nếu version được chỉ định, chỉ thử version đó
            var versionsToTry = version != null
                ? new[] { version }
                : _apiVersions;

            // Thử lần lượt các phiên bản API
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

                    // Lấy ảnh cho mỗi phim
                    if (result?.Data != null)
                    {
                        await EnrichMoviesWithImagesAsync(result.Data);
                    }

                    // Lưu vào cache
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_cacheExpirationMinutes));

                    _logger.LogInformation($"Successfully fetched latest movies using {ver}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error fetching latest movies with {ver}: {ex.Message}. Trying next version...");
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

            if (_cache.TryGetValue(cacheKey, out MovieDetailResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            // Nếu version được chỉ định, chỉ thử version đó
            var versionsToTry = version != null
                ? new[] { version }
                : _apiVersions;

            // Thử lần lượt các phiên bản API
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
                        // Lưu vào cache
                        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_cacheExpirationMinutes));

                        _logger.LogInformation($"Successfully fetched movie detail using {ver}");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error fetching movie detail with {ver}: {ex.Message}. Trying next version...");
                }
            }

            _logger.LogError($"All API versions failed for GetMovieDetailBySlugAsync with slug: {slug}");
            return null;
        }

        public async Task<MovieListResponse> GetRelatedMoviesAsync(string slug, int limit = 6, string version = null)
        {
            string cacheKey = $"related_movies_{slug}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            // Đơn giản hóa bằng cách lấy phim mới nhất thay vì phim liên quan
            var result = await GetLatestMoviesAsync(1, limit, version);

            // Lưu vào cache
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_cacheExpirationMinutes));

            return result;
        }

        public async Task<MovieListResponse> SearchMoviesAsync(string query, int page = 1, int limit = 10)
        {
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

                // Lấy ảnh cho mỗi phim
                if (result?.Data != null)
                {
                    await EnrichMoviesWithImagesAsync(result.Data);
                }

                // Lưu vào cache
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_cacheExpirationMinutes));

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

        public async Task<MovieListResponse> FilterMoviesAsync(string type = null, string genre = null, string country = null, string year = null, int page = 1, int limit = 10)
        {
            string cacheKey = $"filter_movies_{type}_{genre}_{country}_{year}_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
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

                // Lấy ảnh cho mỗi phim
                if (result?.Data != null)
                {
                    await EnrichMoviesWithImagesAsync(result.Data);
                }

                // Lưu vào cache
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_cacheExpirationMinutes));

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

        private async Task EnrichMoviesWithImagesAsync(List<MovieItem> movies)
        {
            var tasks = new List<Task>();

            foreach (var movie in movies)
            {
                tasks.Add(Task.Run(async () => {
                    try
                    {
                        string cacheKey = $"movie_images_{movie.Slug}";

                        if (_cache.TryGetValue(cacheKey, out ImageResponse imageResponse) && imageResponse?.Success == true)
                        {
                            movie.ThumbUrl = imageResponse.SubThumb;
                            movie.PosterUrl = imageResponse.SubPoster;
                            return;
                        }

                        // Sử dụng semaphore để giới hạn số lượng request đồng thời
                        await _semaphore.WaitAsync();

                        try
                        {
                            // Thử lần lượt các phiên bản API
                            foreach (var version in _apiVersions)
                            {
                                try
                                {
                                    using (var client = new HttpClient())
                                    {
                                        client.Timeout = TimeSpan.FromSeconds(10); // Timeout ngắn cho request ảnh
                                        string url = $"{_baseUrl}/get-img/{version}?slug={movie.Slug}";
                                        var response = await client.GetAsync(url);

                                        if (response.IsSuccessStatusCode)
                                        {
                                            var content = await response.Content.ReadAsStringAsync();
                                            imageResponse = JsonSerializer.Deserialize<ImageResponse>(content, _jsonOptions);

                                            if (imageResponse?.Success == true)
                                            {
                                                movie.ThumbUrl = imageResponse.SubThumb;
                                                movie.PosterUrl = imageResponse.SubPoster;

                                                _cache.Set(cacheKey, imageResponse, TimeSpan.FromHours(1));
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Error fetching images for movie {movie.Slug} with {version}: {ex.Message}. Trying next version...");
                                }
                            }
                        }
                        finally
                        {
                            _semaphore.Release();
                        }

                        // Nếu không lấy được ảnh, sử dụng ảnh mặc định
                        if (string.IsNullOrEmpty(movie.ThumbUrl) && string.IsNullOrEmpty(movie.PosterUrl))
                        {
                            movie.ThumbUrl = "/placeholder.svg?height=450&width=300";
                            movie.PosterUrl = "/placeholder.svg?height=450&width=300";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error enriching movie {movie.Slug} with images: {ex.Message}");

                        // Đảm bảo luôn có ảnh mặc định
                        movie.ThumbUrl = "/placeholder.svg?height=450&width=300";
                        movie.PosterUrl = "/placeholder.svg?height=450&width=300";
                    }
                }));
            }

            // Chờ tất cả các task hoàn thành với timeout
            await Task.WhenAny(
                Task.WhenAll(tasks),
                Task.Delay(5000) // Timeout 5 giây
            );
        }

        public async Task<List<string>> GetGenresAsync()
        {
            string cacheKey = "all_genres";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedGenres))
            {
                return cachedGenres;
            }

            // Danh sách thể loại phim phổ biến
            var genres = new List<string>
            {
                "Hành Động", "Tình Cảm", "Hài Hước", "Cổ Trang", "Kinh Dị", "Hình Sự",
                "Chiến Tranh", "Thể Thao", "Võ Thuật", "Viễn Tưởng", "Phiêu Lưu", "Khoa Học",
                "Tâm Lý", "Gia Đình", "Hoạt Hình", "Âm Nhạc", "Lịch Sử", "Thần Thoại"
            };

            _cache.Set(cacheKey, genres, TimeSpan.FromDays(1));

            return genres;
        }

        public async Task<List<string>> GetCountriesAsync()
        {
            string cacheKey = "all_countries";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedCountries))
            {
                return cachedCountries;
            }

            // Danh sách quốc gia phổ biến
            var countries = new List<string>
            {
                "Việt Nam", "Trung Quốc", "Hàn Quốc", "Nhật Bản", "Thái Lan", "Âu Mỹ",
                "Đài Loan", "Hồng Kông", "Ấn Độ", "Philippines", "Quốc gia khác"
            };

            _cache.Set(cacheKey, countries, TimeSpan.FromDays(1));

            return countries;
        }

        public async Task<List<string>> GetYearsAsync()
        {
            string cacheKey = "all_years";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedYears))
            {
                return cachedYears;
            }

            // Tạo danh sách năm từ 2000 đến năm hiện tại
            var years = new List<string>();
            int currentYear = DateTime.Now.Year;

            for (int year = currentYear; year >= 2000; year--)
            {
                years.Add(year.ToString());
            }

            _cache.Set(cacheKey, years, TimeSpan.FromDays(1));

            return years;
        }

        public async Task<List<string>> GetMovieTypesAsync()
        {
            string cacheKey = "all_movie_types";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedTypes))
            {
                return cachedTypes;
            }

            // Danh sách loại phim
            var types = new List<string>
            {
                "Phim lẻ", "Phim bộ", "Phim chiếu rạp", "Phim đang chiếu", "TV shows", "Hoạt hình"
            };

            _cache.Set(cacheKey, types, TimeSpan.FromDays(1));

            return types;
        }
    }

    public class ImageResponse
    {
        public bool Success { get; set; }
        public string SubThumb { get; set; }
        public string SubPoster { get; set; }
    }
}
