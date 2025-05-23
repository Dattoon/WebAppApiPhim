using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Data;

namespace WebAppApiPhim.Services
{
    public class StreamingService : IStreamingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<StreamingService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public StreamingService(
            ApplicationDbContext context,
            IMovieApiService movieApiService,
            ILogger<StreamingService> logger)
        {
            _context = context;
            _movieApiService = movieApiService;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<CachedMovie> GetCachedMovieAsync(string slug)
        {
            try
            {
                return await _context.CachedMovies
                    .Include(m => m.Episodes)
                    .Include(m => m.Statistic)
                    .FirstOrDefaultAsync(m => m.Slug == slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cached movie: {slug}");
                return null;
            }
        }

        public async Task<CachedMovie> CacheMovieAsync(MovieDetailResponse movieDetail)
        {
            try
            {
                var cachedMovie = await _context.CachedMovies
                    .FirstOrDefaultAsync(m => m.Slug == movieDetail.Slug);

                if (cachedMovie == null)
                {
                    // Create new cached movie
                    cachedMovie = new CachedMovie
                    {
                        Slug = movieDetail.Slug,
                        Name = movieDetail.Name,
                        OriginalName = movieDetail.OriginalName ?? movieDetail.Name,
                        Description = movieDetail.Description,
                        PosterUrl = movieDetail.Poster_url ?? movieDetail.Sub_poster,
                        ThumbUrl = movieDetail.Thumb_url ?? movieDetail.Sub_thumb,
                        Year = movieDetail.Year,
                        Type = movieDetail.Format,
                        Country = movieDetail.Countries,
                        Genres = movieDetail.Genres,
                        Director = movieDetail.Director ?? movieDetail.Directors,
                        Actors = movieDetail.Casts ?? movieDetail.Actors,
                        Duration = movieDetail.Time,
                        Quality = movieDetail.Quality,
                        Language = movieDetail.Language,
                        ViewCount = 0,
                        LastUpdated = DateTime.Now,
                        RawData = JsonSerializer.Serialize(movieDetail, _jsonOptions)
                    };

                    _context.CachedMovies.Add(cachedMovie);
                }
                else
                {
                    // Update existing cached movie
                    cachedMovie.Name = movieDetail.Name;
                    cachedMovie.OriginalName = movieDetail.OriginalName ?? movieDetail.Name;
                    cachedMovie.Description = movieDetail.Description;
                    cachedMovie.PosterUrl = movieDetail.Poster_url ?? movieDetail.Sub_poster ?? cachedMovie.PosterUrl;
                    cachedMovie.ThumbUrl = movieDetail.Thumb_url ?? movieDetail.Sub_thumb ?? cachedMovie.ThumbUrl;
                    cachedMovie.Year = movieDetail.Year;
                    cachedMovie.Type = movieDetail.Format;
                    cachedMovie.Country = movieDetail.Countries;
                    cachedMovie.Genres = movieDetail.Genres;
                    cachedMovie.Director = movieDetail.Director ?? movieDetail.Directors;
                    cachedMovie.Actors = movieDetail.Casts ?? movieDetail.Actors;
                    cachedMovie.Duration = movieDetail.Time;
                    cachedMovie.Quality = movieDetail.Quality;
                    cachedMovie.Language = movieDetail.Language;
                    cachedMovie.LastUpdated = DateTime.Now;
                    cachedMovie.RawData = JsonSerializer.Serialize(movieDetail, _jsonOptions);

                    _context.CachedMovies.Update(cachedMovie);
                }

                // Process episodes if available
                if (movieDetail.Episodes != null && movieDetail.Episodes.Any())
                {
                    await ProcessEpisodesAsync(movieDetail.Slug, movieDetail.Episodes);
                }

                await _context.SaveChangesAsync();
                return cachedMovie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error caching movie {movieDetail.Slug}");
                throw;
            }
        }

        public async Task<List<ServerViewModel>> GetEpisodesAsync(string slug)
        {
            try
            {
                var episodes = await _context.CachedEpisodes
                    .Where(e => e.MovieSlug == slug)
                    .OrderBy(e => e.ServerName)
                    .ThenBy(e => e.EpisodeName)
                    .ToListAsync();

                if (!episodes.Any())
                {
                    // If not in cache, get from API
                    var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);
                    if (movieDetail != null)
                    {
                        await CacheMovieAsync(movieDetail);
                        episodes = await _context.CachedEpisodes
                            .Where(e => e.MovieSlug == slug)
                            .OrderBy(e => e.ServerName)
                            .ThenBy(e => e.EpisodeName)
                            .ToListAsync();
                    }
                }

                // Group by server
                var servers = episodes
                    .GroupBy(e => e.ServerName)
                    .Select(g => new ServerViewModel
                    {
                        Name = g.Key,
                        Episodes = g.Select(e => new EpisodeViewModel
                        {
                            Name = e.EpisodeName,
                            Slug = e.EpisodeSlug,
                            EmbedUrl = e.EmbedUrl,
                            M3u8Url = e.M3u8Url,
                            IsWatched = false,
                            WatchedPercentage = 0
                        }).ToList()
                    })
                    .ToList();

                return servers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episodes for movie {slug}");
                return new List<ServerViewModel>();
            }
        }

        public async Task<EpisodeViewModel> GetEpisodeAsync(string slug, string episodeSlug)
        {
            try
            {
                var episode = await _context.CachedEpisodes
                    .FirstOrDefaultAsync(e => e.MovieSlug == slug && e.EpisodeSlug == episodeSlug);

                if (episode == null)
                {
                    // If not in cache, get from API
                    var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);
                    if (movieDetail != null)
                    {
                        await CacheMovieAsync(movieDetail);
                        episode = await _context.CachedEpisodes
                            .FirstOrDefaultAsync(e => e.MovieSlug == slug && e.EpisodeSlug == episodeSlug);
                    }
                }

                if (episode == null)
                    return null;

                return new EpisodeViewModel
                {
                    Name = episode.EpisodeName,
                    Slug = episode.EpisodeSlug,
                    EmbedUrl = episode.EmbedUrl,
                    M3u8Url = episode.M3u8Url,
                    IsWatched = false,
                    WatchedPercentage = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episode {episodeSlug} for movie {slug}");
                return null;
            }
        }

        public async Task<EpisodeProgress> GetEpisodeProgressAsync(string userId, string movieSlug, string episodeSlug)
        {
            try
            {
                return await _context.EpisodeProgresses
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.MovieSlug == movieSlug && p.EpisodeSlug == episodeSlug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episode progress for user {userId}, movie {movieSlug}, episode {episodeSlug}");
                return null;
            }
        }

        public async Task UpdateEpisodeProgressAsync(string userId, string movieSlug, string episodeSlug, double currentTime, double duration)
        {
            try
            {
                var progress = await _context.EpisodeProgresses
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.MovieSlug == movieSlug && p.EpisodeSlug == episodeSlug);

                if (progress == null)
                {
                    // Create new progress
                    progress = new EpisodeProgress
                    {
                        UserId = userId,
                        MovieSlug = movieSlug,
                        EpisodeSlug = episodeSlug,
                        CurrentTime = currentTime,
                        Duration = duration,
                        UpdatedAt = DateTime.Now
                    };

                    _context.EpisodeProgresses.Add(progress);
                }
                else
                {
                    // Update existing progress
                    progress.CurrentTime = currentTime;
                    progress.Duration = duration;
                    progress.UpdatedAt = DateTime.Now;

                    _context.EpisodeProgresses.Update(progress);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating progress for episode {episodeSlug} of movie {movieSlug}");
                throw;
            }
        }

        public async Task IncrementViewCountAsync(string slug)
        {
            try
            {
                var movie = await _context.CachedMovies
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (movie != null)
                {
                    movie.ViewCount++;
                    _context.CachedMovies.Update(movie);
                }

                // Update statistics
                var statistic = await _context.MovieStatistics
                    .FirstOrDefaultAsync(s => s.MovieSlug == slug);

                if (statistic == null)
                {
                    statistic = new MovieStatistic
                    {
                        MovieSlug = slug,
                        ViewCount = 1,
                        FavoriteCount = 0,
                        CommentCount = 0,
                        AverageRating = 0,
                        RatingCount = 0,
                        LastUpdated = DateTime.Now
                    };

                    _context.MovieStatistics.Add(statistic);
                }
                else
                {
                    statistic.ViewCount++;
                    statistic.LastUpdated = DateTime.Now;
                    _context.MovieStatistics.Update(statistic);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing view count for movie {slug}");
                // Don't throw - this is a non-critical operation
            }
        }

        private async Task ProcessEpisodesAsync(string movieSlug, List<object> episodes)
        {
            try
            {
                // Remove old episodes
                var oldEpisodes = await _context.CachedEpisodes
                    .Where(e => e.MovieSlug == movieSlug)
                    .ToListAsync();

                _context.CachedEpisodes.RemoveRange(oldEpisodes);

                // Add new episodes
                foreach (var server in episodes)
                {
                    try
                    {
                        var jsonServer = JsonSerializer.Serialize(server);
                        var serverObj = JsonSerializer.Deserialize<JsonElement>(jsonServer);

                        var serverName = serverObj.GetProperty("server_name").GetString();
                        var items = serverObj.GetProperty("items").EnumerateArray();

                        foreach (var item in items)
                        {
                            var episodeName = item.GetProperty("name").GetString();
                            var episodeSlug = item.GetProperty("slug").GetString();
                            var embedUrl = item.GetProperty("embed").GetString();
                            var m3u8Url = item.GetProperty("m3u8").GetString();

                            var cachedEpisode = new CachedEpisode
                            {
                                MovieSlug = movieSlug,
                                ServerName = serverName,
                                EpisodeName = episodeName,
                                EpisodeSlug = episodeSlug,
                                EmbedUrl = embedUrl,
                                M3u8Url = m3u8Url,
                                LastUpdated = DateTime.Now
                            };

                            _context.CachedEpisodes.Add(cachedEpisode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing episode data for movie {movieSlug}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing episodes for movie {movieSlug}");
                throw;
            }
        }
    }
}
