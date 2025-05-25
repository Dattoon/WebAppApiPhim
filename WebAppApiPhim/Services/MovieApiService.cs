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
        private readonly TimeSpan _cacheTime = TimeSpan.FromHours(1);
        private readonly JsonSerializerOptions _jsonOptions;

        public MovieApiService(
            HttpClient httpClient,
            IMemoryCache memoryCache,
            ILogger<MovieApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 20, string version = "v1")
        {
            string cacheKey = $"latest_movies_{page}_{limit}_{version}";
            if (_memoryCache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation("Cache hit for latest movies: page {page}, version {version}", page, version);
                return cachedResponse;
            }

            try
            {
                string endpoint = $"phim-moi/{version}?page={page}&limit={limit}";

                _logger.LogInformation("Calling API endpoint: {endpoint}", endpoint);
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API returned {statusCode} for endpoint: {endpoint}", response.StatusCode, endpoint);
                    return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw API Response (first 500 chars): {content}",
                    content.Length > 500 ? content.Substring(0, 500) + "..." : content);

                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                var movieListResponse = ParseRealMovieListResponse(root, page, limit, version);

                if (movieListResponse.Data.Any())
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _cacheTime,
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(30)
                    };

                    _memoryCache.Set(cacheKey, movieListResponse, cacheEntryOptions);
                    _logger.LogInformation("Successfully cached {count} movies for page {page}", movieListResponse.Data.Count, page);
                }

                return movieListResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for endpoint: {endpoint}", $"phim-moi/{version}?page={page}&limit={limit}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest movies: page {page}, limit {limit}, version {version}", page, limit, version);
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        private MovieListResponse ParseRealMovieListResponse(JsonElement root, int page, int limit, string version)
        {
            var movieListResponse = new MovieListResponse
            {
                Data = new List<MovieItem>(),
                Pagination = new Pagination
                {
                    CurrentPage = page,
                    TotalPages = 1,
                    TotalItems = 0,
                    Limit = limit
                }
            };

            try
            {
                if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                {
                    movieListResponse.Data = ParseRealMovieItems(dataElement, version);
                }

                if (root.TryGetProperty("pagination", out var paginationElement))
                {
                    movieListResponse.Pagination = ParseRealPagination(paginationElement);
                }

                _logger.LogInformation("Parsed {count} movies from API response", movieListResponse.Data.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing movie list response structure");
            }

            return movieListResponse;
        }

        private List<MovieItem> ParseRealMovieItems(JsonElement itemsElement, string version)
        {
            var movies = new List<MovieItem>();

            try
            {
                foreach (var item in itemsElement.EnumerateArray())
                {
                    var movie = new MovieItem();

                    // Parse based on API version
                    if (version == "v1")
                    {
                        // V1 format: id, name, slug, year, loai_phim, quoc_gia, modified
                        if (item.TryGetProperty("slug", out var slugProp))
                            movie.Slug = slugProp.GetString();

                        if (item.TryGetProperty("name", out var nameProp))
                            movie.Title = nameProp.GetString();

                        if (item.TryGetProperty("year", out var yearProp))
                            movie.Year = yearProp.GetString();

                        if (item.TryGetProperty("modified", out var modifiedProp))
                            movie.Modified = new ModifiedData { Time = modifiedProp.GetString() };
                    }
                    else if (version == "v2")
                    {
                        // V2 format: id, name, slug, year, loai_phim, tmdb_id, quoc_gia, modified_time
                        if (item.TryGetProperty("slug", out var slugProp))
                            movie.Slug = slugProp.GetString();

                        if (item.TryGetProperty("name", out var nameProp))
                            movie.Title = nameProp.GetString();

                        if (item.TryGetProperty("year", out var yearProp))
                            movie.Year = yearProp.GetString();

                        if (item.TryGetProperty("tmdb_id", out var tmdbProp))
                            movie.TmdbId = tmdbProp.GetString();

                        if (item.TryGetProperty("modified_time", out var modifiedTimeProp))
                            movie.Modified = new ModifiedData { Time = modifiedTimeProp.GetString() };
                    }
                    else if (version == "v3")
                    {
                        // V3 format: id, name, slug, year, loai_phim, tmdb_id, quoc_gia, modified_time
                        if (item.TryGetProperty("slug", out var slugProp))
                            movie.Slug = slugProp.GetString();

                        if (item.TryGetProperty("name", out var nameProp))
                            movie.Title = nameProp.GetString();

                        if (item.TryGetProperty("year", out var yearProp))
                            movie.Year = yearProp.GetString();

                        if (item.TryGetProperty("tmdb_id", out var tmdbProp))
                            movie.TmdbId = tmdbProp.GetString();

                        if (item.TryGetProperty("modified_time", out var modifiedTimeProp))
                            movie.Modified = new ModifiedData { Time = modifiedTimeProp.GetString() };
                    }

                    // Get poster URL using the real image API
                    if (!string.IsNullOrEmpty(movie.Slug))
                    {
                        movie.PosterUrl = $"https://api.dulieuphim.ink/get-img/v1?slug={movie.Slug}";
                    }
                    else
                    {
                        movie.PosterUrl = "/placeholder.svg?height=450&width=300";
                    }

                    // Only add if we have essential data
                    if (!string.IsNullOrEmpty(movie.Slug) && !string.IsNullOrEmpty(movie.Title))
                    {
                        movies.Add(movie);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing individual movie items");
            }

            return movies;
        }

        private Pagination ParseRealPagination(JsonElement paginationElement)
        {
            var pagination = new Pagination();

            try
            {
                if (paginationElement.TryGetProperty("current_page", out var currentPageProp))
                    pagination.CurrentPage = currentPageProp.GetInt32();

                if (paginationElement.TryGetProperty("total_pages", out var totalPagesProp))
                    pagination.TotalPages = totalPagesProp.GetInt32();

                if (paginationElement.TryGetProperty("total_items", out var totalItemsProp))
                    pagination.TotalItems = totalItemsProp.GetInt32();

                if (paginationElement.TryGetProperty("limit", out var limitProp))
                    pagination.Limit = limitProp.GetInt32();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing pagination data");
            }

            return pagination;
        }

        public async Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string version = "v1")
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            string cacheKey = $"movie_detail_{slug}_{version}";
            if (_memoryCache.TryGetValue(cacheKey, out MovieDetailResponse cachedMovie))
            {
                _logger.LogInformation("Cache hit for movie detail: {slug}", slug);
                return cachedMovie;
            }

            try
            {
                // Use the correct movie detail endpoint
                string endpoint = $"phim-chi-tiet/{version}?slug={slug}";
                _logger.LogInformation("Fetching movie detail from: {endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Movie detail API returned {statusCode} for slug: {slug}", response.StatusCode, slug);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                var movie = ParseRealMovieDetail(root, slug);

                if (movie != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _cacheTime,
                        Priority = CacheItemPriority.Normal
                    };
                    _memoryCache.Set(cacheKey, movie, cacheEntryOptions);
                    _logger.LogInformation("Fetched and cached movie detail for: {slug}", slug);
                }

                return movie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching movie detail for: {slug}", slug);
                return null;
            }
        }

        private MovieDetailResponse ParseRealMovieDetail(JsonElement root, string slug)
        {
            try
            {
                var movie = new MovieDetailResponse();

                // Parse basic movie information from real API structure
                if (root.TryGetProperty("slug", out var slugProp))
                    movie.Slug = slugProp.GetString();
                else
                    movie.Slug = slug;

                if (root.TryGetProperty("name", out var nameProp))
                    movie.Title = nameProp.GetString();

                if (root.TryGetProperty("description", out var descProp))
                    movie.Description = descProp.GetString();

                if (root.TryGetProperty("poster_url", out var posterProp))
                    movie.PosterUrl = posterProp.GetString();

                if (root.TryGetProperty("thumb_url", out var thumbProp))
                    movie.ThumbUrl = thumbProp.GetString();

                if (root.TryGetProperty("year", out var yearProp))
                    movie.Year = yearProp.GetString();

                if (root.TryGetProperty("director", out var directorProp))
                    movie.Director = directorProp.GetString();

                if (root.TryGetProperty("time", out var timeProp))
                    movie.Duration = timeProp.GetString();

                if (root.TryGetProperty("language", out var langProp))
                    movie.Language = langProp.GetString();

                if (root.TryGetProperty("casts", out var castsProp))
                    movie.Actors = castsProp.GetString();

                if (root.TryGetProperty("countries", out var countriesProp))
                {
                    var countriesStr = countriesProp.GetString();
                    movie.Countries = countriesStr?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
                }

                if (root.TryGetProperty("genres", out var genresProp))
                {
                    var genresStr = genresProp.GetString();
                    movie.Genres = genresStr?.Split(',').Select(g => g.Trim()).ToList() ?? new List<string>();
                }

                // Parse episodes from real API structure
                if (root.TryGetProperty("episodes", out var episodesElement) && episodesElement.ValueKind == JsonValueKind.Array)
                {
                    movie.Episodes = ParseRealEpisodes(episodesElement);
                }

                // Use real image URLs if available, otherwise use API endpoints
                if (string.IsNullOrEmpty(movie.PosterUrl))
                {
                    if (root.TryGetProperty("sub_poster", out var subPosterProp))
                        movie.PosterUrl = subPosterProp.GetString();
                }

                if (string.IsNullOrEmpty(movie.ThumbUrl))
                {
                    if (root.TryGetProperty("sub_thumb", out var subThumbProp))
                        movie.ThumbUrl = subThumbProp.GetString();
                }

                return movie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing movie detail");
                return null;
            }
        }

        private List<Episode> ParseRealEpisodes(JsonElement episodesElement)
        {
            var episodes = new List<Episode>();

            try
            {
                foreach (var serverGroup in episodesElement.EnumerateArray())
                {
                    var episode = new Episode();

                    if (serverGroup.TryGetProperty("server_name", out var serverNameProp))
                        episode.ServerName = serverNameProp.GetString();

                    if (serverGroup.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsElement.EnumerateArray())
                        {
                            var episodeItem = new EpisodeItem();

                            if (item.TryGetProperty("name", out var nameProp))
                                episodeItem.Title = nameProp.GetString();

                            if (item.TryGetProperty("slug", out var slugProp))
                                episodeItem.Id = slugProp.GetString();

                            if (item.TryGetProperty("embed", out var embedProp))
                                episodeItem.Embed = embedProp.GetString();

                            if (item.TryGetProperty("m3u8", out var m3u8Prop))
                                episodeItem.M3u8 = m3u8Prop.GetString();

                            episode.Items.Add(episodeItem);
                        }
                    }

                    episodes.Add(episode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing episodes");
            }

            return episodes;
        }

        public async Task<ImageResponse> GetMovieImagesAsync(string slug)
        {
            try
            {
                var endpoint = $"get-img/v1?slug={slug}";
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    return new ImageResponse { Success = false };
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                var imageResponse = new ImageResponse();

                if (root.TryGetProperty("success", out var successProp))
                    imageResponse.Success = successProp.GetBoolean();

                if (root.TryGetProperty("sub_poster", out var posterProp))
                    imageResponse.SubPoster = posterProp.GetString();

                if (root.TryGetProperty("sub_thumb", out var thumbProp))
                    imageResponse.SubThumb = thumbProp.GetString();

                return imageResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie images for: {slug}", slug);
                return new ImageResponse { Success = false };
            }
        }

        public async Task<MovieListResponse> GetMoviesByCategoryAsync(string category, int page = 1)
        {
            try
            {
                var endpoint = $"phim-data/v1?{category}&page={page}&limit=20";
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                return ParseRealMovieListResponse(root, page, 20, "v1");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movies by category: {category}", category);
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        public async Task<MovieListResponse> GetMoviesByGenreAsync(string genreSlug, int page = 1)
        {
            return await GetMoviesByCategoryAsync($"the_loai={Uri.EscapeDataString(genreSlug)}", page);
        }

        public async Task<MovieListResponse> GetMoviesByCountryAsync(string countrySlug, int page = 1)
        {
            return await GetMoviesByCategoryAsync($"quoc_gia={Uri.EscapeDataString(countrySlug)}", page);
        }

        public async Task<MovieListResponse> GetMoviesByTypeAsync(string typeSlug, int page = 1)
        {
            return await GetMoviesByCategoryAsync($"loai_phim={Uri.EscapeDataString(typeSlug)}", page);
        }

        public async Task<ProductionApiResponse> GetProductionDataAsync(string slug)
        {
            try
            {
                var endpoint = $"get-nha-phat-hanh/{slug}";
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    return new ProductionApiResponse { Success = false };
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                var productionResponse = new ProductionApiResponse();

                if (root.TryGetProperty("success", out var successProp))
                    productionResponse.Success = successProp.GetBoolean();

                // Parse production companies and streaming platforms
                // Implementation details based on the real API structure

                return productionResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting production data for: {slug}", slug);
                return new ProductionApiResponse { Success = false };
            }
        }

        public async Task<List<string>> GetGenresAsync()
        {
            return new List<string>
            {
                "Hành Động", "Tình Cảm", "Hài Hước", "Cổ Trang", "Tâm Lý", "Hình Sự",
                "Chiến Tranh", "Thể Thao", "Võ Thuật", "Viễn Tưởng", "Phiêu Lưu", "Khoa Học",
                "Kinh Dị", "Âm Nhạc", "Thần Thoại", "Tài Liệu", "Gia Đình", "Học Đường"
            };
        }

        public async Task<List<string>> GetCountriesAsync()
        {
            return new List<string>
            {
                "Việt Nam", "Trung Quốc", "Hàn Quốc", "Nhật Bản", "Thái Lan", "Âu Mỹ",
                "Ấn Độ", "Nga", "Philippines", "Hong Kong", "Đài Loan", "Khác"
            };
        }

        public async Task<List<string>> GetMovieTypesAsync()
        {
            return new List<string> { "Phim Lẻ", "Phim Bộ", "Hoạt Hình", "TV Shows" };
        }
    }
}
