using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;
using System.Net.Http;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<MoviesController> _logger;
        private readonly HttpClient _httpClient;

        // API version priority order
        private readonly string[] _apiVersions = { "v3", "v2", "v1" };

        public MoviesController(
            ApplicationDbContext context,
            IMovieApiService movieApiService,
            ILogger<MoviesController> logger,
            HttpClient httpClient)
        {
            _context = context;
            _movieApiService = movieApiService;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Get movies with intelligent API version fallback
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MovieListResponse>> GetMovies(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? preferredVersion = null)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                // Try database first for performance
                var dbResult = await GetMoviesFromDatabase(page, limit);
                if (dbResult != null) return dbResult;

                // Database empty or insufficient, try API with version fallback
                _logger.LogInformation("Database insufficient, fetching from API with version fallback...");
                return await GetMoviesFromApiWithFallback(page, limit, preferredVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movies");
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get movie detail with intelligent API version fallback
        /// </summary>
        [HttpGet("{slug}")]
        public async Task<ActionResult> GetMovie(string slug, [FromQuery] string? preferredVersion = null)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest("Movie slug is required");

            try
            {
                // Try database first
                var cachedMovie = await GetMovieFromDatabase(slug);
                if (cachedMovie != null)
                {
                    await IncrementViewCount(slug);
                    return Ok(cachedMovie);
                }

                // Not in database, try API with version fallback
                var apiMovie = await GetMovieFromApiWithFallback(slug, preferredVersion);
                if (apiMovie != null)
                {
                    // Cache the movie for future requests
                    await CacheMovieFromApi(apiMovie);
                    return Ok(apiMovie);
                }

                return NotFound(new { message = $"Movie '{slug}' not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movie: {slug}", slug);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Manual sync with intelligent version fallback
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncMovies(
            [FromQuery] int pages = 3,
            [FromQuery] string? preferredVersion = null)
        {
            try
            {
                var syncedCount = 0;
                var skippedCount = 0;
                var errors = new List<string>();

                for (int page = 1; page <= pages; page++)
                {
                    try
                    {
                        var apiResult = await GetMoviesFromApiWithFallback(page, 20, preferredVersion);

                        if (apiResult?.Value is MovieListResponse movieList && movieList.Data?.Any() == true)
                        {
                            var result = await SafeCacheMoviesToDatabase(movieList.Data);
                            syncedCount += result.synced;
                            skippedCount += result.skipped;

                            if (result.errors.Any())
                            {
                                errors.AddRange(result.errors.Select(e => $"Page {page}: {e}"));
                            }

                            _logger.LogInformation("Synced page {page}: {synced} new, {skipped} skipped",
                                page, result.synced, result.skipped);
                        }
                        else
                        {
                            errors.Add($"Page {page}: No data returned from any API version");
                        }
                    }
                    catch (Exception pageEx)
                    {
                        var error = $"Page {page}: {pageEx.Message}";
                        errors.Add(error);
                        _logger.LogError(pageEx, "Error syncing page {page}", page);
                    }
                }

                return Ok(new
                {
                    message = $"Sync completed. {syncedCount} synced, {skippedCount} skipped",
                    syncedCount,
                    skippedCount,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing movies");
                return StatusCode(500, new { message = "Sync failed", error = ex.Message });
            }
        }

        [HttpGet("test-api-versions")]
        public async Task<IActionResult> TestApiVersions([FromQuery] string? slug = null)
        {
            try
            {
                var results = new List<ApiVersionTestResult>();

                foreach (var version in _apiVersions)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(slug))
                        {
                            // Test movie list API
                            var listResult = await _movieApiService.GetLatestMoviesAsync(1, 5, version);
                            results.Add(new ApiVersionTestResult
                            {
                                Version = version,
                                Type = "movie_list",
                                Success = listResult?.Data?.Any() == true,
                                Count = listResult?.Data?.Count ?? 0,
                                Error = null
                            });
                        }
                        else
                        {
                            // Test specific movie API
                            var movieResult = await _movieApiService.GetMovieDetailBySlugAsync(slug, version);
                            results.Add(new ApiVersionTestResult
                            {
                                Version = version,
                                Type = "movie_detail",
                                Success = movieResult != null,
                                Slug = slug,
                                Title = movieResult?.Title,
                                Error = null
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ApiVersionTestResult
                        {
                            Version = version,
                            Type = string.IsNullOrEmpty(slug) ? "movie_list" : "movie_detail",
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }

                var recommendedVersion = results.FirstOrDefault(r => r.Success)?.Version;
                return Ok(new { results, recommendedVersion });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task<ActionResult<MovieListResponse>?> GetMoviesFromDatabase(int page, int limit)
        {
            var totalItems = await _context.CachedMovies.CountAsync();
            if (totalItems == 0) return null;

            var movies = await _context.CachedMovies
                .AsNoTracking()
                .OrderByDescending(m => m.LastUpdated)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new MovieListResponse
            {
                Data = movies.Select(m => new MovieItem
                {
                    Slug = m.Slug,
                    Title = m.Title,
                    PosterUrl = m.PosterUrl,
                    Year = m.Year,
                    TmdbId = m.TmdbId,
                    Modified = new ModifiedData { Time = m.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") }
                }).ToList(),
                Pagination = new Pagination
                {
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalItems / limit),
                    TotalItems = totalItems,
                    Limit = limit
                }
            });
        }

        private async Task<CachedMovie?> GetMovieFromDatabase(string slug)
        {
            return await _context.CachedMovies
                .AsNoTracking()
                .Include(m => m.Episodes)
                .FirstOrDefaultAsync(m => m.Slug == slug);
        }

        private async Task<ActionResult<MovieListResponse>> GetMoviesFromApiWithFallback(int page, int limit, string? preferredVersion)
        {
            var versionsToTry = GetVersionsToTry(preferredVersion);

            foreach (var version in versionsToTry)
            {
                try
                {
                    _logger.LogDebug("Trying movie list API version {version}", version);
                    var result = await _movieApiService.GetLatestMoviesAsync(page, limit, version);

                    if (result?.Data?.Any() == true)
                    {
                        _logger.LogInformation("Successfully retrieved movies using API version {version}", version);
                        return Ok(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("API version {version} failed for movie list: {error}", version, ex.Message);
                    continue;
                }
            }

            _logger.LogError("All API versions failed for movie list");
            return StatusCode(503, new { message = "All API versions unavailable" });
        }

        private async Task<MovieDetailResponse?> GetMovieFromApiWithFallback(string slug, string? preferredVersion)
        {
            var versionsToTry = GetVersionsToTry(preferredVersion);

            foreach (var version in versionsToTry)
            {
                try
                {
                    _logger.LogDebug("Trying movie detail API version {version} for {slug}", version, slug);
                    var result = await _movieApiService.GetMovieDetailBySlugAsync(slug, version);

                    if (result != null)
                    {
                        _logger.LogInformation("Successfully retrieved movie {slug} using API version {version}", slug, version);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("API version {version} failed for movie {slug}: {error}", version, slug, ex.Message);
                    continue;
                }
            }

            _logger.LogError("All API versions failed for movie {slug}", slug);
            return null;
        }

        private string[] GetVersionsToTry(string? preferredVersion)
        {
            if (!string.IsNullOrEmpty(preferredVersion) && _apiVersions.Contains(preferredVersion))
            {
                // Put preferred version first, then others
                return new[] { preferredVersion }
                    .Concat(_apiVersions.Where(v => v != preferredVersion))
                    .ToArray();
            }

            return _apiVersions;
        }

        private async Task<(int synced, int skipped, List<string> errors)> SafeCacheMoviesToDatabase(List<MovieItem> apiMovies)
        {
            var syncedCount = 0;
            var skippedCount = 0;
            var errors = new List<string>();

            var apiSlugs = apiMovies
                .Where(m => !string.IsNullOrWhiteSpace(m.Slug) && !string.IsNullOrWhiteSpace(m.Title))
                .Select(m => m.Slug)
                .Distinct()
                .ToList();

            var existingSlugs = await _context.CachedMovies
                .Where(m => apiSlugs.Contains(m.Slug))
                .Select(m => m.Slug)
                .ToListAsync();

            foreach (var apiMovie in apiMovies)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(apiMovie.Slug) || string.IsNullOrWhiteSpace(apiMovie.Title))
                    {
                        errors.Add("Skipping movie with missing slug or title");
                        continue;
                    }

                    if (existingSlugs.Contains(apiMovie.Slug))
                    {
                        skippedCount++;
                        continue;
                    }

                    var imageUrls = await GetBestImageUrls(apiMovie.Slug);

                    var cachedMovie = new CachedMovie
                    {
                        Slug = apiMovie.Slug,
                        Title = apiMovie.Title,
                        PosterUrl = apiMovie.PosterUrl ?? imageUrls.poster,
                        ThumbUrl = imageUrls.thumb,
                        Year = apiMovie.Year ?? "",
                        TmdbId = apiMovie.TmdbId ?? "",
                        Views = 0,
                        LastUpdated = DateTime.UtcNow,
                        Description = "",
                        Director = "",
                        Duration = "",
                        Language = "",
                        Resolution = "",
                        TrailerUrl = "",
                        Rating = null,
                        RawData = ""
                    };

                    _context.CachedMovies.Add(cachedMovie);
                    await _context.SaveChangesAsync();

                    syncedCount++;
                    existingSlugs.Add(apiMovie.Slug);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to cache movie {apiMovie.Slug}: {ex.Message}");
                    _context.ChangeTracker.Clear();
                }
            }

            return (syncedCount, skippedCount, errors);
        }

        private async Task<(string poster, string thumb)> GetBestImageUrls(string slug)
        {
            foreach (var version in _apiVersions)
            {
                try
                {
                    var imageApiUrl = $"https://api.dulieuphim.ink/get-img/{version}?slug={slug}";
                    var response = await _httpClient.GetAsync(imageApiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        var imageResponse = System.Text.Json.JsonSerializer.Deserialize<ImageApiResponse>(jsonContent, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (imageResponse?.Success == true &&
                            (!string.IsNullOrEmpty(imageResponse.SubPoster) || !string.IsNullOrEmpty(imageResponse.SubThumb)))
                        {
                            return (
                                imageResponse.SubPoster ?? "/placeholder.svg?height=450&width=300",
                                imageResponse.SubThumb ?? "/placeholder.svg?height=450&width=300"
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Image API version {version} failed for {slug}: {error}", version, slug, ex.Message);
                    continue;
                }
            }

            return ("/placeholder.svg?height=450&width=300", "/placeholder.svg?height=450&width=300");
        }

        private async Task IncrementViewCount(string slug)
        {
            try
            {
                var movie = await _context.CachedMovies.FirstOrDefaultAsync(m => m.Slug == slug);
                if (movie != null)
                {
                    movie.Views++;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count for {slug}", slug);
            }
        }

        private async Task CacheMovieFromApi(MovieDetailResponse apiMovie)
        {
            try
            {
                var existingMovie = await _context.CachedMovies.FirstOrDefaultAsync(m => m.Slug == apiMovie.Slug);
                if (existingMovie == null)
                {
                    var imageUrls = await GetBestImageUrls(apiMovie.Slug);

                    var cachedMovie = new CachedMovie
                    {
                        Slug = apiMovie.Slug,
                        Title = apiMovie.Title,
                        Description = apiMovie.Description ?? "",
                        PosterUrl = apiMovie.PosterUrl ?? imageUrls.poster,
                        ThumbUrl = apiMovie.ThumbUrl ?? imageUrls.thumb,
                        Year = apiMovie.Year ?? "",
                        Director = apiMovie.Director ?? "",
                        Duration = apiMovie.Duration ?? "",
                        Language = apiMovie.Language ?? "",
                        TmdbId = apiMovie.TmdbId ?? "",
                        Rating = apiMovie.Rating,
                        TrailerUrl = apiMovie.TrailerUrl ?? "",
                        Views = 0,
                        LastUpdated = DateTime.UtcNow,
                        RawData = System.Text.Json.JsonSerializer.Serialize(apiMovie)
                    };

                    _context.CachedMovies.Add(cachedMovie);
                    await _context.SaveChangesAsync();

                    // ✅ THÊM: Lưu episodes
                    if (apiMovie.Episodes?.Any() == true)
                    {
                        await SaveEpisodesToDatabase(apiMovie.Slug, apiMovie.Episodes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching movie from API: {slug}", apiMovie.Slug);
            }
        }
        [HttpPost("sync-episodes/{slug}")]
        public async Task<IActionResult> SyncEpisodes(string slug)
        {
            try
            {
                // Lấy chi tiết phim từ API
                var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);
                if (movieDetail == null)
                {
                    return NotFound($"Không tìm thấy phim: {slug}");
                }

                // Kiểm tra phim có trong database chưa
                var existingMovie = await _context.CachedMovies.FirstOrDefaultAsync(m => m.Slug == slug);
                if (existingMovie == null)
                {
                    return NotFound($"Phim {slug} chưa có trong database. Hãy sync phim trước.");
                }

                // Lưu episodes
                if (movieDetail.Episodes?.Any() == true)
                {
                    await SaveEpisodesToDatabase(slug, movieDetail.Episodes);

                    var episodeCount = await _context.CachedEpisodes.CountAsync(e => e.MovieSlug == slug);

                    return Ok(new
                    {
                        message = $"Đã sync {episodeCount} tập phim cho {slug}",
                        movieSlug = slug,
                        movieTitle = existingMovie.Title,
                        episodeCount
                    });
                }
                else
                {
                    return Ok(new
                    {
                        message = $"Phim {slug} không có tập phim",
                        movieSlug = slug,
                        movieTitle = existingMovie.Title,
                        episodeCount = 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing episodes for: {slug}", slug);
                return StatusCode(500, new { message = "Lỗi khi sync episodes", error = ex.Message });
            }
        }

        private async Task SaveEpisodesToDatabase(string movieSlug, List<Episode> episodes)
        {
            try
            {
                // Xóa episodes cũ nếu có
                var existingEpisodes = await _context.CachedEpisodes
                    .Where(e => e.MovieSlug == movieSlug)
                    .ToListAsync();

                if (existingEpisodes.Any())
                {
                    _context.CachedEpisodes.RemoveRange(existingEpisodes);
                }

                // Lưu episodes mới
                foreach (var serverGroup in episodes)
                {
                    foreach (var episodeItem in serverGroup.Items)
                    {
                        // Tạo số tập từ title
                        int episodeNumber;
                        if (!int.TryParse(episodeItem.Title, out episodeNumber))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(episodeItem.Title, @"\d+");
                            if (match.Success)
                            {
                                int.TryParse(match.Value, out episodeNumber);
                            }
                            else
                            {
                                episodeNumber = 1; // Default
                            }
                        }

                        // Kiểm tra episode đã tồn tại chưa
                        var existingEpisode = await _context.CachedEpisodes
                            .FirstOrDefaultAsync(e => e.MovieSlug == movieSlug && e.EpisodeNumber == episodeNumber);

                        if (existingEpisode == null)
                        {
                            var newEpisode = new CachedEpisode
                            {
                                Id = Guid.NewGuid().ToString(),
                                MovieSlug = movieSlug,
                                EpisodeNumber = episodeNumber,
                                Title = $"Tập {episodeNumber}",
                                Url = episodeItem.M3u8 ?? episodeItem.Embed ?? "",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _context.CachedEpisodes.Add(newEpisode);
                            await _context.SaveChangesAsync();

                            // Lưu servers
                            if (!string.IsNullOrEmpty(episodeItem.M3u8))
                            {
                                _context.EpisodeServers.Add(new EpisodeServer
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    EpisodeId = newEpisode.Id,
                                    ServerName = $"{serverGroup.ServerName} - M3U8",
                                    ServerUrl = episodeItem.M3u8
                                });
                            }

                            if (!string.IsNullOrEmpty(episodeItem.Embed))
                            {
                                _context.EpisodeServers.Add(new EpisodeServer
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    EpisodeId = newEpisode.Id,
                                    ServerName = $"{serverGroup.ServerName} - Embed",
                                    ServerUrl = episodeItem.Embed
                                });
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved episodes for movie: {movieSlug}", movieSlug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving episodes for movie: {movieSlug}", movieSlug);
            }
        }
        #endregion
    }

    public class ApiVersionTestResult
    {
        public string Version { get; set; } = "";
        public string Type { get; set; } = "";
        public bool Success { get; set; }
        public int Count { get; set; }
        public string? Slug { get; set; }
        public string? Title { get; set; }
        public string? Error { get; set; }
    }

    public class ImageApiResponse
    {
        public bool Success { get; set; }
        public string? SubThumb { get; set; }
        public string? SubPoster { get; set; }
    }
}
