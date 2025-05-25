using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;
using System.Text.Json;

namespace WebAppApiPhim.Services
{
    public class StreamingService : IStreamingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StreamingService> _logger;
        private readonly IMemoryCache _memoryCache;

        public StreamingService(
            ApplicationDbContext context,
            ILogger<StreamingService> logger,
            IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<CachedMovie> GetCachedMovieAsync(string slug)
        {
            try
            {
                return await _context.CachedMovies
                    .Include(m => m.Episodes)
                    .Include(m => m.MovieGenreMappings)
                        .ThenInclude(mgm => mgm.Genre)
                    .Include(m => m.MovieCountryMappings)
                        .ThenInclude(mcm => mcm.Country)
                    .FirstOrDefaultAsync(m => m.Slug == slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached movie: {slug}", slug);
                return null;
            }
        }

        public async Task<List<EpisodeViewModel>> GetEpisodesAsync(string movieSlug)
        {
            try
            {
                var episodes = await _context.CachedEpisodes
                    .Where(e => e.MovieSlug == movieSlug)
                    .Include(e => e.EpisodeServers)
                    .OrderBy(e => e.EpisodeNumber)
                    .ToListAsync();

                return episodes.Select(e => new EpisodeViewModel
                {
                    Id = e.Id,
                    EpisodeNumber = e.EpisodeNumber,
                    Title = e.Title,
                    Url = e.Url,
                    Servers = e.EpisodeServers.Select(s => new ServerViewModel
                    {
                        Name = s.ServerName,
                        Url = s.ServerUrl
                    }).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episodes for movie: {movieSlug}", movieSlug);
                return new List<EpisodeViewModel>();
            }
        }

        public async Task<EpisodeViewModel> GetEpisodeAsync(string movieSlug, string episodeId)
        {
            try
            {
                var episode = await _context.CachedEpisodes
                    .Include(e => e.EpisodeServers)
                    .FirstOrDefaultAsync(e => e.MovieSlug == movieSlug && e.Id == episodeId);

                if (episode == null) return null;

                return new EpisodeViewModel
                {
                    Id = episode.Id,
                    EpisodeNumber = episode.EpisodeNumber,
                    Title = episode.Title,
                    Url = episode.Url,
                    Servers = episode.EpisodeServers.Select(s => new ServerViewModel
                    {
                        Name = s.ServerName,
                        Url = s.ServerUrl
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episode: {episodeId} for movie: {movieSlug}", episodeId, movieSlug);
                return null;
            }
        }

        public async Task IncrementViewCountAsync(string slug)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var dailyView = await _context.DailyViews
                    .FirstOrDefaultAsync(dv => dv.MovieSlug == slug && dv.Date == today);

                if (dailyView != null)
                {
                    dailyView.ViewCount++;
                    dailyView.Views++;
                    _context.DailyViews.Update(dailyView);
                }
                else
                {
                    dailyView = new DailyView
                    {
                        Id = Guid.NewGuid().ToString(),
                        MovieSlug = slug,
                        Date = today,
                        ViewCount = 1,
                        Views = 1
                    };
                    _context.DailyViews.Add(dailyView);
                }

                // Update movie views
                var movie = await _context.CachedMovies.FirstOrDefaultAsync(m => m.Slug == slug);
                if (movie != null)
                {
                    movie.Views++;
                    _context.CachedMovies.Update(movie);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count for: {slug}", slug);
                throw;
            }
        }

        public async Task<CachedMovie> CacheMovieAsync(MovieDetailResponse movieDetail)
        {
            try
            {
                var existingMovie = await _context.CachedMovies
                    .FirstOrDefaultAsync(m => m.Slug == movieDetail.Slug);

                if (existingMovie != null)
                {
                    // Update existing movie
                    existingMovie.Title = movieDetail.Title;
                    existingMovie.Description = movieDetail.Description;
                    existingMovie.PosterUrl = movieDetail.PosterUrl;
                    existingMovie.ThumbUrl = movieDetail.ThumbUrl;
                    existingMovie.Year = movieDetail.Year;
                    existingMovie.Director = movieDetail.Director;
                    existingMovie.Duration = movieDetail.Duration;
                    existingMovie.Language = movieDetail.Language;
                    existingMovie.TmdbId = movieDetail.TmdbId;
                    existingMovie.Rating = movieDetail.Rating;
                    existingMovie.TrailerUrl = movieDetail.TrailerUrl;
                    existingMovie.LastUpdated = DateTime.UtcNow;
                    existingMovie.RawData = JsonSerializer.Serialize(movieDetail);

                    _context.CachedMovies.Update(existingMovie);
                    await _context.SaveChangesAsync();
                    return existingMovie;
                }
                else
                {
                    // Create new movie
                    var cachedMovie = new CachedMovie
                    {
                        Slug = movieDetail.Slug,
                        Title = movieDetail.Title,
                        Description = movieDetail.Description,
                        PosterUrl = movieDetail.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                        ThumbUrl = movieDetail.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                        Year = movieDetail.Year,
                        Director = movieDetail.Director,
                        Duration = movieDetail.Duration,
                        Language = movieDetail.Language,
                        TmdbId = movieDetail.TmdbId,
                        Rating = movieDetail.Rating,
                        TrailerUrl = movieDetail.TrailerUrl,
                        Views = movieDetail.Views,
                        LastUpdated = DateTime.UtcNow,
                        RawData = JsonSerializer.Serialize(movieDetail)
                    };

                    _context.CachedMovies.Add(cachedMovie);
                    await _context.SaveChangesAsync();
                    return cachedMovie;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching movie: {slug}", movieDetail.Slug);
                throw;
            }
        }

        public async Task<EpisodeProgress> GetEpisodeProgressAsync(string userId, string movieSlug, string episodeId)
        {
            try
            {
                return await _context.EpisodeProgresses
                    .FirstOrDefaultAsync(ep => ep.UserId == Guid.Parse(userId) &&
                                             ep.Episode.MovieSlug == movieSlug &&
                                             ep.EpisodeId == episodeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episode progress");
                return null;
            }
        }

        public async Task UpdateEpisodeProgressAsync(string userId, string movieSlug, string episodeId, double currentTime, double duration)
        {
            try
            {
                var progress = await _context.EpisodeProgresses
                    .FirstOrDefaultAsync(ep => ep.UserId == Guid.Parse(userId) &&
                                             ep.Episode.MovieSlug == movieSlug &&
                                             ep.EpisodeId == episodeId);

                var watchedPercentage = (currentTime / duration) * 100;

                if (progress != null)
                {
                    progress.WatchedPercentage = watchedPercentage;
                    progress.LastWatched = DateTime.UtcNow;
                    _context.EpisodeProgresses.Update(progress);
                }
                else
                {
                    progress = new EpisodeProgress
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = Guid.Parse(userId),
                        EpisodeId = episodeId,
                        WatchedPercentage = watchedPercentage,
                        LastWatched = DateTime.UtcNow
                    };
                    _context.EpisodeProgresses.Add(progress);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating episode progress");
                throw;
            }
        }

        // Implement remaining methods with basic functionality
        public async Task<List<ServerViewModel>> GetServersAsync(string movieSlug, int episodeNumber)
        {
            return new List<ServerViewModel>
            {
                new ServerViewModel { Name = "Server 1", Url = "/placeholder-video.mp4" },
                new ServerViewModel { Name = "Server 2", Url = "/placeholder-video.mp4" }
            };
        }

        public async Task<string> GetStreamingUrlAsync(string movieSlug, int episodeNumber, string serverName)
        {
            return "/placeholder-video.mp4";
        }

        public async Task<bool> ValidateStreamingUrlAsync(string url)
        {
            return !string.IsNullOrEmpty(url);
        }
    }
}
