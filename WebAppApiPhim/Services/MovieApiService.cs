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
using System.Text.Json.Serialization;

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
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5);

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
            httpClient.BaseAddress = new Uri(_baseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _dbContext = dbContext;
            _scopeFactory = scopeFactory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<PaginatedResponse<MovieListItemViewModel>> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = null)
        {
            string cacheKey = $"latest_movies_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out PaginatedResponse<MovieListItemViewModel> cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            var versionsToTry = version != null ? new[] { version } : _apiVersions;

            foreach (var ver in versionsToTry)
            {
                try
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        string url = $"/phim-moi/{ver}?page={page}&limit={limit}";
                        _logger.LogInformation($"Fetching latest movies from {url}");

                        var response = await _httpClient.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"Raw response for latest movies: {content}");
                        var result = JsonSerializer.Deserialize<ApiResponse<MovieListResponse>>(content, _jsonOptions);

                        if (result?.Success != true || result.Data?.Data == null)
                        {
                            _logger.LogWarning($"Invalid response from {url}");
                            continue;
                        }

                        await EnrichMoviesWithImagesAsync(result.Data.Data);

                        var viewModelList = result.Data.Data.Select(movie => new MovieListItemViewModel
                        {
                            Slug = movie.Slug ?? string.Empty,
                            Name = movie.Name ?? string.Empty,
                            OriginalName = movie.OriginalName,
                            Year = movie.Year,
                            ThumbUrl = movie.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                            PosterUrl = movie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                            Type = movie.Loai_phim,
                            Quality = null,
                            ViewCount = 0,
                            AverageRating = 0,
                            IsFavorite = false,
                            WatchedPercentage = null
                        }).ToList();

                        var paginatedResponse = new PaginatedResponse<MovieListItemViewModel>
                        {
                            Data = viewModelList,
                            CurrentPage = result.Data.Pagination.Current_page,
                            TotalPages = result.Data.Pagination.Total_pages,
                            TotalItems = result.Data.Pagination.Total_items,
                            PageSize = result.Data.Pagination.Limit
                        };

                        _cache.Set(cacheKey, paginatedResponse, new MemoryCacheEntryOptions()
                            .SetSize(1)
                            .SetAbsoluteExpiration(_shortCacheTime));

                        _logger.LogInformation($"Successfully fetched latest movies using {ver}");
                        return paginatedResponse;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error fetching latest movies with {ver}: {ex.Message}");
                }
            }

            _logger.LogError("All API versions failed for GetLatestMoviesAsync");
            return new PaginatedResponse<MovieListItemViewModel>
            {
                Data = new List<MovieListItemViewModel>(),
                CurrentPage = page,
                TotalPages = 1,
                TotalItems = 0,
                PageSize = limit
            };
        }

        public async Task<MovieDetailViewModel> GetMovieDetailBySlugAsync(string slug, string version = null)
        {
            string cacheKey = $"movie_detail_{slug}";

            if (_cache.TryGetValue(cacheKey, out MovieDetailViewModel cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            var cachedMovie = await _dbContext.CachedMovies
                .Include(m => m.Statistic)
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (cachedMovie != null && !string.IsNullOrEmpty(cachedMovie.RawData))
            {
                try
                {
                    var movieDetail = JsonSerializer.Deserialize<MovieDetailResponse>(cachedMovie.RawData, _jsonOptions);
                    if (movieDetail != null)
                    {
                        var viewModel = MapToMovieDetailViewModel(movieDetail, cachedMovie);
                        _cache.Set(cacheKey, viewModel, new MemoryCacheEntryOptions()
                            .SetSize(1)
                            .SetAbsoluteExpiration(_mediumCacheTime));
                        return viewModel;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error deserializing cached movie data for {slug}");
                }
            }

            var versionsToTry = version != null ? new[] { version } : _apiVersions;

            foreach (var ver in versionsToTry)
            {
                try
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        string url = $"/phim-chi-tiet/{ver}?slug={slug}";
                        _logger.LogInformation($"Fetching movie detail from {url}");

                        var response = await _httpClient.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"Raw response for movie detail {slug}: {content}");
                        var result = JsonSerializer.Deserialize<ApiResponse<MovieDetailResponse>>(content, _jsonOptions);

                        if (result?.Success != true || result.Data == null)
                        {
                            _logger.LogWarning($"Invalid response from {url}");
                            continue;
                        }

                        result.Data.ThumbUrl = !string.IsNullOrEmpty(result.Data.Sub_thumb) ? result.Data.Sub_thumb :
                                              !string.IsNullOrEmpty(result.Data.Thumb_url) ? result.Data.Thumb_url :
                                              result.Data.ThumbUrl;
                        result.Data.PosterUrl = !string.IsNullOrEmpty(result.Data.Sub_poster) ? result.Data.Sub_poster :
                                               !string.IsNullOrEmpty(result.Data.Poster_url) ? result.Data.Poster_url :
                                               result.Data.PosterUrl;

                        if (string.IsNullOrEmpty(result.Data.ThumbUrl) || string.IsNullOrEmpty(result.Data.PosterUrl))
                        {
                            var movieItem = new MovieItem { Slug = slug };
                            await EnrichMovieWithImageAsync(movieItem);
                            result.Data.ThumbUrl = movieItem.ThumbUrl;
                            result.Data.PosterUrl = movieItem.PosterUrl;
                        }

                        var viewModel = MapToMovieDetailViewModel(result.Data, cachedMovie);

                        _cache.Set(cacheKey, viewModel, new MemoryCacheEntryOptions()
                            .SetSize(1)
                            .SetAbsoluteExpiration(_mediumCacheTime));

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await MovieDatabaseHelper.StoreMovieInDatabaseAsync(result.Data, _dbContext, _logger, _jsonOptions);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error storing movie {slug} in database");
                            }
                        });

                        _logger.LogInformation($"Successfully fetched movie detail using {ver}");
                        return viewModel;
                    }
                    finally
                    {
                        _semaphore.Release();
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

        public async Task<PaginatedResponse<MovieListItemViewModel>> SearchMoviesAsync(string query, int page = 1, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetLatestMoviesAsync(page, limit);
            }

            string cacheKey = $"search_movies_{query}_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out PaginatedResponse<MovieListItemViewModel> cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    string url = $"/phim-data/v1?name={Uri.EscapeDataString(query)}&page={page}&limit={limit}";
                    _logger.LogInformation($"Searching movies from {url}");

                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Raw response for search movies: {content}");
                    var result = JsonSerializer.Deserialize<ApiResponse<MovieListResponse>>(content, _jsonOptions);

                    if (result?.Success != true || result.Data?.Data == null)
                    {
                        _logger.LogWarning($"Invalid response from {url}");
                        return new PaginatedResponse<MovieListItemViewModel>
                        {
                            Data = new List<MovieListItemViewModel>(),
                            CurrentPage = page,
                            TotalPages = 1,
                            TotalItems = 0,
                            PageSize = limit
                        };
                    }

                    await EnrichMoviesWithImagesAsync(result.Data.Data);

                    var viewModelList = result.Data.Data.Select(movie => new MovieListItemViewModel
                    {
                        Slug = movie.Slug ?? string.Empty,
                        Name = movie.Name ?? string.Empty,
                        OriginalName = movie.OriginalName,
                        Year = movie.Year,
                        ThumbUrl = movie.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                        PosterUrl = movie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                        Type = movie.Loai_phim,
                        Quality = null,
                        ViewCount = 0,
                        AverageRating = 0,
                        IsFavorite = false,
                        WatchedPercentage = null
                    }).ToList();

                    var paginatedResponse = new PaginatedResponse<MovieListItemViewModel>
                    {
                        Data = viewModelList,
                        CurrentPage = result.Data.Pagination.Current_page,
                        TotalPages = result.Data.Pagination.Total_pages,
                        TotalItems = result.Data.Pagination.Total_items,
                        PageSize = result.Data.Pagination.Limit
                    };

                    _cache.Set(cacheKey, paginatedResponse, new MemoryCacheEntryOptions()
                        .SetSize(1)
                        .SetAbsoluteExpiration(_shortCacheTime));

                    return paginatedResponse;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching movies: {ex.Message}");
                return new PaginatedResponse<MovieListItemViewModel>
                {
                    Data = new List<MovieListItemViewModel>(),
                    CurrentPage = page,
                    TotalPages = 1,
                    TotalItems = 0,
                    PageSize = limit
                };
            }
        }

        public async Task<PaginatedResponse<MovieListItemViewModel>> FilterMoviesAsync(
            string type = null, string genre = null, string country = null, string year = null, int page = 1, int limit = 10)
        {
            string cacheKey = $"filter_movies_{type}_{genre}_{country}_{year}_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out PaginatedResponse<MovieListItemViewModel> cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    string url = $"/phim-data/v1?page={page}&limit={limit}";

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
                    _logger.LogDebug($"Raw response for filter movies: {content}");
                    var result = JsonSerializer.Deserialize<ApiResponse<MovieListResponse>>(content, _jsonOptions);

                    if (result?.Success != true || result.Data?.Data == null)
                    {
                        _logger.LogWarning($"Invalid response from {url}");
                        return new PaginatedResponse<MovieListItemViewModel>
                        {
                            Data = new List<MovieListItemViewModel>(),
                            CurrentPage = page,
                            TotalPages = 1,
                            TotalItems = 0,
                            PageSize = limit
                        };
                    }

                    await EnrichMoviesWithImagesAsync(result.Data.Data);

                    var viewModelList = result.Data.Data.Select(movie => new MovieListItemViewModel
                    {
                        Slug = movie.Slug ?? string.Empty,
                        Name = movie.Name ?? string.Empty,
                        OriginalName = movie.OriginalName,
                        Year = movie.Year,
                        ThumbUrl = movie.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                        PosterUrl = movie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                        Type = movie.Loai_phim,
                        Quality = null,
                        ViewCount = 0,
                        AverageRating = 0,
                        IsFavorite = false,
                        WatchedPercentage = null
                    }).ToList();

                    var paginatedResponse = new PaginatedResponse<MovieListItemViewModel>
                    {
                        Data = viewModelList,
                        CurrentPage = result.Data.Pagination.Current_page,
                        TotalPages = result.Data.Pagination.Total_pages,
                        TotalItems = result.Data.Pagination.Total_items,
                        PageSize = result.Data.Pagination.Limit
                    };

                    _cache.Set(cacheKey, paginatedResponse, new MemoryCacheEntryOptions()
                        .SetSize(1)
                        .SetAbsoluteExpiration(_shortCacheTime));

                    return paginatedResponse;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error filtering movies: {ex.Message}");
                return new PaginatedResponse<MovieListItemViewModel>
                {
                    Data = new List<MovieListItemViewModel>(),
                    CurrentPage = page,
                    TotalPages = 1,
                    TotalItems = 0,
                    PageSize = limit
                };
            }
        }

        public async Task<PaginatedResponse<MovieListItemViewModel>> GetRelatedMoviesAsync(string slug, int limit = 6, string version = null)
        {
            string cacheKey = $"related_movies_{slug}_{limit}";

            if (_cache.TryGetValue(cacheKey, out PaginatedResponse<MovieListItemViewModel> cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
                var movieDetail = await GetMovieDetailBySlugAsync(slug, version);

                if (movieDetail != null && !string.IsNullOrEmpty(movieDetail.Genres))
                {
                    var genres = movieDetail.Genres.Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(genres))
                    {
                        var relatedByGenre = await FilterMoviesAsync(
                            type: movieDetail.Type,
                            genre: genres,
                            limit: limit);

                        if (relatedByGenre?.Data != null && relatedByGenre.Data.Any())
                        {
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

                var result = await GetLatestMoviesAsync(1, limit + 1, version);

                result.Data = result.Data
                    .Where(m => m.Slug != slug)
                    .Take(limit)
                    .ToList();

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

        public async Task<List<string>> GetGenresAsync()
        {
            string cacheKey = "all_genres";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedGenres))
            {
                return cachedGenres;
            }

            var genres = await _dbContext.Genres
                .Where(g => g.IsActive)
                .Select(g => g.Name)
                .ToListAsync();

            if (!genres.Any())
            {
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

            var countries = await _dbContext.Countries
                .Where(c => c.IsActive)
                .Select(c => c.Name)
                .ToListAsync();

            if (!countries.Any())
            {
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

            var types = await _dbContext.MovieTypes
                .Where(t => t.IsActive)
                .Select(t => t.Name)
                .ToListAsync();

            if (!types.Any())
            {
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

        private async Task EnrichMoviesWithImagesAsync(List<MovieItem> movies)
        {
            const int batchSize = 3;
            for (int i = 0; i < movies.Count; i += batchSize)
            {
                var batch = movies.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(movie => EnrichMovieWithImageAsync(movie)).ToList();
                var completedTasks = await Task.WhenAny(
                    Task.WhenAll(tasks),
                    Task.Delay(15000)
                );
                if (completedTasks != Task.WhenAll(tasks))
                {
                    _logger.LogWarning($"Timeout occurred while enriching images for batch starting at index {i}");
                }
            }
        }

        private async Task EnrichMovieWithImageAsync(MovieItem movie)
        {
            string cacheKey = $"movie_images_{movie.Slug}";

            try
            {
                // 1. Lấy từ MemoryCache
                if (_cache.TryGetValue(cacheKey, out ImageResponse cachedImage) && cachedImage?.Success == true
                    && !string.IsNullOrEmpty(cachedImage.SubThumb) && !string.IsNullOrEmpty(cachedImage.SubPoster))
                {
                    movie.ThumbUrl = cachedImage.SubThumb;
                    movie.PosterUrl = cachedImage.SubPoster;
                    _logger.LogInformation($"Cache hit for images of {movie.Slug}");
                    return;
                }

                // 2. Lấy từ DB
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cachedMovie = await scopedDbContext.CachedMovies
                        .FirstOrDefaultAsync(m => m.Slug == movie.Slug);
                    if (cachedMovie != null && !string.IsNullOrEmpty(cachedMovie.ThumbUrl)
                        && !string.IsNullOrEmpty(cachedMovie.PosterUrl))
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
                        _logger.LogInformation($"DB hit for images of {movie.Slug}");
                        return;
                    }
                }

                // 3. Gọi API get-img với retry
                string[] imageApiVersions = { "v2", "v3", "v1" };
                foreach (var ver in imageApiVersions)
                {
                    var imgResult = await FetchImageWithRetryAsync(movie.Slug, ver);
                    if (imgResult != null && imgResult.Success && !string.IsNullOrEmpty(imgResult.SubThumb)
                        && !string.IsNullOrEmpty(imgResult.SubPoster))
                    {
                        movie.ThumbUrl = imgResult.SubThumb;
                        movie.PosterUrl = imgResult.SubPoster;
                        _cache.Set(cacheKey, imgResult, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _longCacheTime,
                            Size = 1
                        });
                        _logger.LogInformation($"Fetched and cached images for {movie.Slug} from v{ver}");
                        return;
                    }
                }

                // 4. Fallback ảnh mặc định
                _logger.LogWarning($"No valid image data found for {movie.Slug}, using default images");
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

        private async Task<ImageResponse> FetchImageWithRetryAsync(string slug, string version, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        string imageUrl = $"/get-img/{version}?slug={slug}";
                        _logger.LogInformation($"Fetching image for {slug} from {imageUrl}, attempt {attempt}");
                        var response = await _httpClient.GetAsync(imageUrl);
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"Raw image response for {slug} (v{version}, attempt {attempt}): {content}");
                        var imgResult = JsonSerializer.Deserialize<ImageResponse>(content, _jsonOptions);
                        if (imgResult != null && imgResult.Success && !string.IsNullOrEmpty(imgResult.SubThumb)
                            && !string.IsNullOrEmpty(imgResult.SubPoster))
                        {
                            return imgResult;
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Image fetch failed for {slug} (version {version}, attempt {attempt})");
                    if (attempt == maxRetries) return null;
                    await Task.Delay(1000 * attempt);
                }
            }
            return null;
        }

        private MovieDetailViewModel MapToMovieDetailViewModel(MovieDetailResponse movie, CachedMovie cachedMovie)
        {
            return new MovieDetailViewModel
            {
                Slug = movie.Slug ?? string.Empty,
                Name = movie.Name ?? string.Empty,
                OriginalName = movie.OriginalName,
                Description = movie.Description,
                Year = movie.Year,
                ThumbUrl = movie.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                PosterUrl = movie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                Type = movie.Type ?? movie.Format,
                Country = movie.Countries,
                Genres = movie.Genres,
                Director = movie.Director ?? movie.Directors,
                Actors = movie.Actors ?? movie.Casts,
                Duration = movie.Time,
                Quality = movie.Quality,
                Language = movie.Language,
                ViewCount = cachedMovie?.ViewCount ?? 0,
                AverageRating = cachedMovie?.Statistic?.AverageRating ?? 0,
                RatingCount = cachedMovie?.Statistic?.RatingCount ?? 0,
                IsFavorite = false,
                UserRating = null
            };
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