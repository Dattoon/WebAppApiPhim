using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.BackgroundServices
{
    public class SyncMoviesService : BackgroundService
    {
        private readonly ILogger<SyncMoviesService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SyncMoviesService(ILogger<SyncMoviesService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncMoviesService is starting.");

            // Wait 1 minute before first run to let the app fully start
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SafeSyncMovies(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during movie synchronization.");
                }

                // Run every 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }

            _logger.LogInformation("SyncMoviesService is stopping.");
        }

        private async Task SafeSyncMovies(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting intelligent movie synchronization...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var movieApiService = scope.ServiceProvider.GetRequiredService<IMovieApiService>();

            // Create HttpClient for image API calls
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // API version priority order
            var apiVersions = new[] { "v3", "v2", "v1" };

            try
            {
                var totalSynced = 0;
                var totalSkipped = 0;
                var totalErrors = 0;

                // Sync only 3 pages (60 movies) to be safe
                for (int page = 1; page <= 3; page++)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        _logger.LogInformation("Processing page {page}...", page);

                        // Try API versions with fallback
                        MovieListResponse? apiResult = null;
                        string? successfulVersion = null;

                        foreach (var version in apiVersions)
                        {
                            try
                            {
                                _logger.LogDebug("Trying API version {version} for page {page}", version, page);
                                apiResult = await movieApiService.GetLatestMoviesAsync(page, 20, version);

                                if (apiResult?.Data?.Any() == true)
                                {
                                    successfulVersion = version;
                                    _logger.LogInformation("Successfully retrieved page {page} using API version {version}", page, version);
                                    break;
                                }
                            }
                            catch (Exception versionEx)
                            {
                                _logger.LogWarning("API version {version} failed for page {page}: {error}", version, page, versionEx.Message);
                                continue;
                            }
                        }

                        if (apiResult?.Data == null || !apiResult.Data.Any())
                        {
                            _logger.LogWarning("No data returned from any API version for page {page}", page);
                            continue;
                        }

                        // Get existing slugs from database to avoid duplicates
                        var apiSlugs = apiResult.Data
                            .Where(m => !string.IsNullOrWhiteSpace(m.Slug) && !string.IsNullOrWhiteSpace(m.Title))
                            .Select(m => m.Slug)
                            .Distinct()
                            .ToList();

                        var existingSlugs = await context.CachedMovies
                            .Where(m => apiSlugs.Contains(m.Slug))
                            .Select(m => m.Slug)
                            .ToListAsync(stoppingToken);

                        var newMoviesToAdd = apiResult.Data
                            .Where(apiMovie => !string.IsNullOrWhiteSpace(apiMovie.Slug) &&
                                             !string.IsNullOrWhiteSpace(apiMovie.Title) &&
                                             !existingSlugs.Contains(apiMovie.Slug))
                            .ToList();

                        _logger.LogInformation("Page {page}: Found {total} movies, {existing} already exist, {new} new movies to add (using {version})",
                            page, apiResult.Data.Count, existingSlugs.Count, newMoviesToAdd.Count, successfulVersion);

                        // Process each movie individually to avoid batch failures
                        foreach (var apiMovie in newMoviesToAdd)
                        {
                            try
                            {
                                // Double-check to avoid race conditions
                                var exists = await context.CachedMovies
                                    .AnyAsync(m => m.Slug == apiMovie.Slug, stoppingToken);

                                if (exists)
                                {
                                    totalSkipped++;
                                    continue;
                                }

                                // Get best image URLs using intelligent fallback
                                var imageUrls = await GetBestImageUrls(apiMovie.Slug, httpClient, apiVersions);

                                var cachedMovie = new CachedMovie
                                {
                                    Slug = apiMovie.Slug,
                                    Title = apiMovie.Title,
                                    PosterUrl = apiMovie.PosterUrl ?? imageUrls.poster,
                                    ThumbUrl = imageUrls.thumb,
                                    LastUpdated = DateTime.UtcNow,
                                    Views = 0,

                                    // Safe defaults for required fields
                                    Description = "",
                                    Director = "",
                                    Duration = "",
                                    Language = "",
                                    Year = apiMovie.Year ?? "",
                                    TmdbId = apiMovie.TmdbId ?? "",
                                    Resolution = "",
                                    TrailerUrl = "",
                                    Rating = null,
                                    RawData = ""
                                };

                                context.CachedMovies.Add(cachedMovie);
                                await context.SaveChangesAsync(stoppingToken);

                                totalSynced++;
                                _logger.LogDebug("Successfully synced movie: {slug} with images from API", apiMovie.Slug);
                            }
                            catch (Exception movieEx)
                            {
                                totalErrors++;
                                _logger.LogWarning(movieEx, "Failed to sync individual movie: {slug}", apiMovie.Slug);

                                // Reset context state to continue with next movie
                                context.ChangeTracker.Clear();
                            }
                        }

                        totalSkipped += existingSlugs.Count;
                        _logger.LogInformation("Completed page {page}: {synced} synced, {skipped} skipped, {errors} errors",
                            page, newMoviesToAdd.Count - totalErrors, existingSlugs.Count, totalErrors);

                        // Delay between pages to be respectful to the API
                        await Task.Delay(2000, stoppingToken);
                    }
                    catch (Exception pageEx)
                    {
                        _logger.LogError(pageEx, "Failed to process page {page}", page);
                    }
                }

                _logger.LogInformation("Movie synchronization completed. Total: {synced} synced, {skipped} skipped, {errors} errors",
                    totalSynced, totalSkipped, totalErrors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during movie synchronization");
            }
        }

        /// <summary>
        /// Get best image URLs using intelligent API version fallback
        /// </summary>
        private async Task<(string poster, string thumb)> GetBestImageUrls(string slug, HttpClient httpClient, string[] apiVersions)
        {
            foreach (var version in apiVersions)
            {
                try
                {
                    var imageApiUrl = $"https://api.dulieuphim.ink/get-img/{version}?slug={slug}";
                    var response = await httpClient.GetAsync(imageApiUrl);

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
                            _logger.LogDebug("Found images for {slug} using version {version}", slug, version);
                            return (
                                imageResponse.SubPoster ?? "/placeholder.svg?height=450&width=300",
                                imageResponse.SubThumb ?? "/placeholder.svg?height=450&width=300"
                            );
                        }
                        else
                        {
                            _logger.LogDebug("Image API {version} returned null images for {slug}", version, slug);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Image API version {version} failed for {slug}: {error}", version, slug, ex.Message);
                    continue;
                }
            }

            _logger.LogWarning("No images found for {slug} from any API version, using placeholder", slug);
            return ("/placeholder.svg?height=450&width=300", "/placeholder.svg?height=450&width=300");
        }
    }

    /// <summary>
    /// Response model for the image API
    /// </summary>
    public class ImageApiResponse
    {
        public bool Success { get; set; }
        public string? SubThumb { get; set; }
        public string? SubPoster { get; set; }
    }
}
