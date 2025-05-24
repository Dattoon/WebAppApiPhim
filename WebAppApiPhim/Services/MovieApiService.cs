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
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        public async Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = null)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 50) limit = 10;

            string cacheKey = $"latest_movies_{page}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            var versionsToTry = version != null ? new[] { version } : _apiVersions;

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

                    if (result?.Data != null && result.Data.Any())
                    {
                        await EnrichMoviesWithImagesAsync(result.Data);
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _shortCacheTime,
                        Size = EstimateCacheSize(result) // Ước lượng kích thước dựa trên dữ liệu
                    };

                    _cache.Set(cacheKey, result, cacheEntryOptions);
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
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            string cacheKey = $"movie_detail_{slug}";

            if (_cache.TryGetValue(cacheKey, out MovieDetailResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            var cachedMovie = await _dbContext.CachedMovies
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (cachedMovie != null && !string.IsNullOrEmpty(cachedMovie.RawData))
            {
                try
                {
                    var movieDetail = JsonSerializer.Deserialize<MovieDetailResponse>(cachedMovie.RawData, _jsonOptions);
                    if (movieDetail != null)
                    {
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _mediumCacheTime,
                            Size = EstimateCacheSize(movieDetail)
                        };
                        _cache.Set(cacheKey, movieDetail, cacheOptions);
                        return movieDetail;
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
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Timeout 10 giây
                    string url = $"{_baseUrl}/phim-chi-tiet/{ver}?slug={slug}";
                    _logger.LogInformation($"Fetching movie detail from {url}");

                    var response = await _httpClient.GetAsync(url, cts.Token);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync(cts.Token);
                    var result = JsonSerializer.Deserialize<MovieDetailResponse>(content, _jsonOptions);

                    if (result != null)
                    {
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _mediumCacheTime,
                            Size = EstimateCacheSize(result)
                        };
                        _cache.Set(cacheKey, result, cacheOptions);

                        await StoreMovieInDatabaseAsync(result);
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
                throw new ArgumentException("Query is required", nameof(query));

            if (page < 1) page = 1;
            if (limit < 1 || limit > 50) limit = 10;

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

                if (result?.Data != null && result.Data.Any())
                {
                    await EnrichMoviesWithImagesAsync(result.Data);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _shortCacheTime,
                    Size = EstimateCacheSize(result)
                };
                _cache.Set(cacheKey, result, cacheEntryOptions);

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
            if (page < 1) page = 1;
            if (limit < 1 || limit > 50) limit = 10;

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

                if (result?.Data != null && result.Data.Any())
                {
                    await EnrichMoviesWithImagesAsync(result.Data);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _shortCacheTime,
                    Size = EstimateCacheSize(result)
                };
                _cache.Set(cacheKey, result, cacheEntryOptions);

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
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            if (limit < 1 || limit > 20) limit = 6;

            string cacheKey = $"related_movies_{slug}_{limit}";

            if (_cache.TryGetValue(cacheKey, out MovieListResponse cachedResponse))
            {
                _logger.LogInformation($"Cache hit for {cacheKey}");
                return cachedResponse;
            }

            try
            {
                var movieDetail = await GetMovieDetailBySlugAsync(slug);

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
                            relatedByGenre.Data = relatedByGenre.Data
                                .Where(m => m.Slug != slug)
                                .Take(limit)
                                .ToList();

                            if (relatedByGenre.Data.Any())
                            {
                                var dbCacheOptions = new MemoryCacheEntryOptions 
                                {
                                    AbsoluteExpirationRelativeToNow = _shortCacheTime,
                                    Size = EstimateCacheSize(relatedByGenre)
                                };
                                _cache.Set(cacheKey, relatedByGenre, dbCacheOptions);
                                return relatedByGenre;
                            }
                        }
                    }
                }

                var result = await GetLatestMoviesAsync(1, limit + 1, version);

                if (result?.Data != null)
                {
                    result.Data = result.Data
                        .Where(m => m.Slug != slug)
                        .Take(limit)
                        .ToList();
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _shortCacheTime,
                    Size = EstimateCacheSize(result)
                };
                _cache.Set(cacheKey, result, cacheEntryOptions);

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

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _longCacheTime,
                Size = EstimateCacheSize(genres)
            };
            _cache.Set(cacheKey, genres, cacheEntryOptions);

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

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _longCacheTime,
                Size = EstimateCacheSize(countries)
            };
            _cache.Set(cacheKey, countries, cacheEntryOptions);

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

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _longCacheTime,
                Size = EstimateCacheSize(years)
            };
            _cache.Set(cacheKey, years, cacheEntryOptions);

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

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _longCacheTime,
                Size = EstimateCacheSize(types)
            };
            _cache.Set(cacheKey, types, cacheEntryOptions);

            return types;
        }

        private async Task EnrichMoviesWithImagesAsync(List<MovieItem> movies)
        {
            const int batchSize = 5;

            for (int i = 0; i < movies.Count; i += batchSize)
            {
                var batch = movies.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(movie => EnrichMovieWithImageAsync(movie)).ToList();

                await Task.WhenAll(tasks); // Đảm bảo tất cả task hoàn thành
            }
        }

        private async Task EnrichMovieWithImageAsync(MovieItem movie)
        {
            string cacheKey = $"movie_images_{movie.Slug}";

            try
            {
                if (_cache.TryGetValue(cacheKey, out ImageResponse cachedImage) && cachedImage?.Success == true)
                {
                    movie.ThumbUrl = cachedImage.SubThumb;
                    movie.PosterUrl = cachedImage.SubPoster;
                    return;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cachedMovie = await scopedDbContext.CachedMovies.FirstOrDefaultAsync(m => m.Slug == movie.Slug);
                    if (cachedMovie != null && !string.IsNullOrEmpty(cachedMovie.ThumbUrl))
                    {
                        movie.ThumbUrl = cachedMovie.ThumbUrl;
                        movie.PosterUrl = cachedMovie.PosterUrl;

                        var cacheEntryOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _longCacheTime,
                            Size = 1024 // Kích thước cố định cho image response
                        };
                        _cache.Set(cacheKey, new ImageResponse { Success = true, SubThumb = cachedMovie.ThumbUrl, SubPoster = cachedMovie.PosterUrl }, cacheEntryOptions);
                        return;
                    }
                }

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

                        if (imgResult != null && imgResult.Success)
                        {
                            movie.ThumbUrl = imgResult.SubThumb;
                            movie.PosterUrl = imgResult.SubPoster;

                            var cacheEntryOptions = new MemoryCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = _longCacheTime,
                                Size = 1024
                            };
                            _cache.Set(cacheKey, imgResult, cacheEntryOptions);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Image fetch failed for {movie.Slug} (version {ver})");
                    }
                }

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

        private async Task StoreMovieInDatabaseAsync(MovieDetailResponse movie)
        {
            try
            {
                var existingMovie = await _dbContext.CachedMovies
                    .Include(m => m.Episodes)
                    .Include(m => m.Statistic)
                    .FirstOrDefaultAsync(m => m.Slug == movie.Slug);

                if (existingMovie == null)
                {
                    var newMovie = new CachedMovie
                    {
                        Slug = movie.Slug,
                        Name = movie.Name,
                        OriginalName = movie.OriginalName,
                        Year = movie.Year,
                        ThumbUrl = movie.Sub_thumb ?? movie.Thumb_url ?? "/placeholder.svg?height=450&width=300",
                        PosterUrl = movie.Sub_poster ?? movie.Poster_url ?? "/placeholder.svg?height=450&width=300",
                        Description = movie.Description,
                        Type = movie.Format ?? movie.Type,
                        Country = movie.Countries,
                        Genres = movie.Genres,
                        Director = movie.Director ?? movie.Directors,
                        Actors = movie.Casts ?? movie.Actors,
                        Duration = movie.Time,
                        Quality = movie.Quality,
                        Language = movie.Language,
                        ViewCount = movie.View,
                        LastUpdated = DateTime.Now,
                        RawData = JsonSerializer.Serialize(movie, _jsonOptions)
                    };

                    newMovie.Statistic = new MovieStatistic
                    {
                        MovieSlug = movie.Slug,
                        ViewCount = movie.View,
                        LastUpdated = DateTime.Now
                    };

                    if (movie.Episodes != null)
                    {
                        foreach (var episodeObj in movie.Episodes)
                        {
                            if (episodeObj is Dictionary<string, object> episodeDict &&
                                episodeDict.ContainsKey("server_name") && episodeDict.ContainsKey("items"))
                            {
                                string serverName = episodeDict["server_name"].ToString();
                                var items = episodeDict["items"] as List<object>;

                                if (items != null)
                                {
                                    foreach (var item in items)
                                    {
                                        if (item is Dictionary<string, object> episodeItem)
                                        {
                                            newMovie.Episodes.Add(new CachedEpisode
                                            {
                                                MovieSlug = movie.Slug,
                                                ServerName = serverName,
                                                EpisodeName = episodeItem.GetValueOrDefault("name")?.ToString(),
                                                EpisodeSlug = episodeItem.GetValueOrDefault("slug")?.ToString(),
                                                EmbedUrl = episodeItem.GetValueOrDefault("embed")?.ToString(),
                                                M3u8Url = episodeItem.GetValueOrDefault("m3u8")?.ToString(),
                                                LastUpdated = DateTime.Now
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _dbContext.CachedMovies.Add(newMovie);
                }
                else
                {
                    existingMovie.Name = movie.Name;
                    existingMovie.OriginalName = movie.OriginalName;
                    existingMovie.Year = movie.Year;
                    existingMovie.ThumbUrl = movie.Sub_thumb ?? movie.Thumb_url ?? existingMovie.ThumbUrl;
                    existingMovie.PosterUrl = movie.Sub_poster ?? movie.Poster_url ?? existingMovie.PosterUrl;
                    existingMovie.Description = movie.Description;
                    existingMovie.Type = movie.Format ?? movie.Type;
                    existingMovie.Country = movie.Countries;
                    existingMovie.Genres = movie.Genres;
                    existingMovie.Director = movie.Director ?? movie.Directors;
                    existingMovie.Actors = movie.Casts ?? movie.Actors;
                    existingMovie.Duration = movie.Time;
                    existingMovie.Quality = movie.Quality;
                    existingMovie.Language = movie.Language;
                    existingMovie.ViewCount = movie.View;
                    existingMovie.LastUpdated = DateTime.Now;
                    existingMovie.RawData = JsonSerializer.Serialize(movie, _jsonOptions);

                    if (existingMovie.Statistic != null)
                    {
                        existingMovie.Statistic.ViewCount = movie.View;
                        existingMovie.Statistic.LastUpdated = DateTime.Now;
                    }

                    if (movie.Episodes != null)
                    {
                        _dbContext.CachedEpisodes.RemoveRange(existingMovie.Episodes);
                        existingMovie.Episodes.Clear();

                        foreach (var episodeObj in movie.Episodes)
                        {
                            if (episodeObj is Dictionary<string, object> episodeDict &&
                                episodeDict.ContainsKey("server_name") && episodeDict.ContainsKey("items"))
                            {
                                string serverName = episodeDict["server_name"].ToString();
                                var items = episodeDict["items"] as List<object>;

                                if (items != null)
                                {
                                    foreach (var item in items)
                                    {
                                        if (item is Dictionary<string, object> episodeItem)
                                        {
                                            existingMovie.Episodes.Add(new CachedEpisode
                                            {
                                                MovieSlug = movie.Slug,
                                                ServerName = serverName,
                                                EpisodeName = episodeItem.GetValueOrDefault("name")?.ToString(),
                                                EpisodeSlug = episodeItem.GetValueOrDefault("slug")?.ToString(),
                                                EmbedUrl = episodeItem.GetValueOrDefault("embed")?.ToString(),
                                                M3u8Url = episodeItem.GetValueOrDefault("m3u8")?.ToString(),
                                                LastUpdated = DateTime.Now
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Successfully stored movie {movie.Slug} in database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error storing movie {movie.Slug} in database");
                throw;
            }
        }

        private int EstimateCacheSize(object data)
        {
            // Ước lượng kích thước dựa trên kích thước chuỗi JSON của dữ liệu
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            return json.Length * sizeof(char); // Ước lượng kích thước byte
        }
    }
}