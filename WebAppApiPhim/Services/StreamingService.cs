using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{


    public class StreamingService : IStreamingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMovieApiService _movieApiService;
        private readonly IMetadataService _metadataService;
        private readonly ILogger<StreamingService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public StreamingService(
            ApplicationDbContext context,
            IMovieApiService movieApiService,
            IMetadataService metadataService,
            ILogger<StreamingService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _movieApiService = movieApiService ?? throw new ArgumentNullException(nameof(movieApiService));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
        }

        public async Task<CachedMovie> GetCachedMovieAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            try
            {
                var movie = await _context.CachedMovies
                    .AsNoTracking()
                    .Include(m => m.Episodes).ThenInclude(e => e.EpisodeServers)
                    .Include(m => m.Statistic)
                    .Include(m => m.MovieGenreMappings).ThenInclude(mgm => mgm.Genre)
                    .Include(m => m.MovieCountryMappings).ThenInclude(mcm => mcm.Country)
                    .Include(m => m.MovieTypeMappings).ThenInclude(mtm => mtm.MovieType)
                    .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
                    .Include(m => m.MovieProductionCompanies).ThenInclude(mpc => mpc.ProductionCompany)
                    .Include(m => m.MovieStreamingPlatforms).ThenInclude(msp => msp.StreamingPlatform)
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (movie == null)
                {
                    _logger.LogWarning($"Movie with slug {slug} not found in cache.");
                }

                return movie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cached movie: {slug}");
                throw new Exception($"Failed to retrieve movie with slug {slug}", ex);
            }
        }

        public async Task<CachedMovie> CacheMovieAsync(MovieDetailResponse movieDetail)
        {
            if (movieDetail == null || string.IsNullOrWhiteSpace(movieDetail.Slug))
                throw new ArgumentException("Movie detail or slug is required", nameof(movieDetail));

            try
            {
                var cachedMovie = await _context.CachedMovies
                    .Include(m => m.Episodes).ThenInclude(e => e.EpisodeServers)
                    .Include(m => m.MovieGenreMappings)
                    .Include(m => m.MovieCountryMappings)
                    .Include(m => m.MovieTypeMappings)
                    .Include(m => m.MovieActors)
                    .Include(m => m.MovieProductionCompanies)
                    .Include(m => m.MovieStreamingPlatforms)
                    .Include(m => m.Statistic)
                    .FirstOrDefaultAsync(m => m.Slug == movieDetail.Slug);

                if (cachedMovie == null)
                {
                    cachedMovie = new CachedMovie
                    {
                        Slug = movieDetail.Slug,
                        Title = movieDetail.Title,
                        Description = movieDetail.Description,
                        PosterUrl = movieDetail.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                        ThumbUrl = movieDetail.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                        Year = movieDetail.Year,
                        Duration = movieDetail.Duration,
                        Language = movieDetail.Language,
                        Director = movieDetail.Director,
                        TmdbId = movieDetail.TmdbId,
                        Views = movieDetail.View, 
                        LastUpdated = DateTime.UtcNow,
                        RawData = JsonSerializer.Serialize(movieDetail, _jsonOptions),
                        MovieGenreMappings = new List<MovieGenreMapping>(),
                        MovieCountryMappings = new List<MovieCountryMapping>(),
                        MovieTypeMappings = new List<MovieTypeMapping>(),
                        MovieActors = new List<MovieActor>(),
                        Episodes = new List<CachedEpisode>(),
                        MovieProductionCompanies = new List<MovieProductionCompany>(),
                        MovieStreamingPlatforms = new List<MovieStreamingPlatform>(),
                        Statistic = new MovieStatistic
                        {
                            MovieSlug = movieDetail.Slug,
                            Views = movieDetail.View,
                            LastUpdated = DateTime.UtcNow
                        }
                    };

                    _context.CachedMovies.Add(cachedMovie);
                }
                else
                {
                    cachedMovie.Title = movieDetail.Title;
                    cachedMovie.Description = movieDetail.Description;
                    cachedMovie.PosterUrl = movieDetail.PosterUrl ?? cachedMovie.PosterUrl;
                    cachedMovie.ThumbUrl = movieDetail.ThumbUrl ?? cachedMovie.ThumbUrl;
                    cachedMovie.Year = movieDetail.Year;
                    cachedMovie.Duration = movieDetail.Duration;
                    cachedMovie.Language = movieDetail.Language;
                    cachedMovie.Director = movieDetail.Director;
                    cachedMovie.TmdbId = movieDetail.TmdbId;
                    cachedMovie.Views = movieDetail.View; // Consistent with long
                    cachedMovie.LastUpdated = DateTime.UtcNow;
                    cachedMovie.RawData = JsonSerializer.Serialize(movieDetail, _jsonOptions);

                    if (cachedMovie.Statistic == null)
                    {
                        cachedMovie.Statistic = new MovieStatistic
                        {
                            MovieSlug = movieDetail.Slug,
                            Views = movieDetail.View, // Consistent with long
                            LastUpdated = DateTime.UtcNow
                        };
                    }
                    else
                    {
                        cachedMovie.Statistic.Views = movieDetail.View; // Consistent with long
                        cachedMovie.Statistic.LastUpdated = DateTime.UtcNow;
                    }

                    _context.CachedMovies.Update(cachedMovie);
                }

                // Xóa các ánh xạ cũ trước khi cập nhật
                cachedMovie.MovieGenreMappings.Clear();
                cachedMovie.MovieCountryMappings.Clear();
                cachedMovie.MovieTypeMappings.Clear();
                cachedMovie.MovieActors.Clear();

                // Cập nhật metadata (genres, countries, types)
                await _metadataService.UpdateMetadataAsync(movieDetail, cachedMovie.Slug);

                // Xử lý diễn viên
                if (!string.IsNullOrEmpty(movieDetail.Actors))
                {
                    var actorNames = movieDetail.Actors.Split(", ", StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim())
                        .Distinct()
                        .ToList();

                    foreach (var actorName in actorNames)
                    {
                        var actor = await _context.Actors
                            .FirstOrDefaultAsync(a => a.Name == actorName);
                        if (actor == null)
                        {
                            actor = new Actor { Name = actorName };
                            _context.Actors.Add(actor);
                            await _context.SaveChangesAsync();
                        }

                        cachedMovie.MovieActors.Add(new MovieActor
                        {
                            MovieSlug = cachedMovie.Slug,
                            ActorId = actor.Id
                        });
                    }
                }

                // Xử lý tập phim
                if (movieDetail.Episodes != null && movieDetail.Episodes.Any())
                {
                    await ProcessEpisodesAsync(cachedMovie.Slug, movieDetail.Episodes);
                }

                // Xử lý dữ liệu bổ sung (diễn viên, nhà sản xuất, nền tảng phát trực tuyến)
                await ProcessAdditionalDataAsync(cachedMovie, movieDetail);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully cached movie {movieDetail.Slug}");
                return cachedMovie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error caching movie {movieDetail.Slug}");
                throw new Exception($"Failed to cache movie with slug {movieDetail.Slug}", ex);
            }
        }

        public async Task<List<ServerViewModel>> GetEpisodesAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            try
            {
                var episodes = await _context.CachedEpisodes
                    .AsNoTracking()
                    .Include(e => e.EpisodeServers)
                    .Where(e => e.MovieSlug == slug)
                    .OrderBy(e => e.EpisodeNumber)
                    .ToListAsync();

                if (!episodes.Any())
                {
                    var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);
                    if (movieDetail != null)
                    {
                        await CacheMovieAsync(movieDetail);
                        episodes = await _context.CachedEpisodes
                            .AsNoTracking()
                            .Include(e => e.EpisodeServers)
                            .Where(e => e.MovieSlug == slug)
                            .OrderBy(e => e.EpisodeNumber)
                            .ToListAsync();
                    }
                }

                var servers = episodes
                    .SelectMany(e => e.EpisodeServers, (e, s) => new { Episode = e, Server = s })
                    .GroupBy(x => x.Server.ServerName)
                    .Select(g => new ServerViewModel
                    {
                        Name = g.Key,
                        Url = g.First().Server.ServerUrl,
                        Servers = g.Select(x => new ServerViewModel
                        {
                            Name = x.Episode.Title,
                            Url = x.Episode.Url,
                            EpisodeNumber = x.Episode.EpisodeNumber
                        }).OrderBy(x => x.EpisodeNumber).ToList()
                    })
                    .ToList();

                if (!servers.Any())
                {
                    _logger.LogWarning($"No episodes found for movie {slug}");
                }

                return servers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episodes for movie {slug}");
                throw new Exception($"Failed to retrieve episodes for movie with slug {slug}", ex);
            }
        }
        public async Task<EpisodeViewModel> GetEpisodeAsync(string slug, string episodeId)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(episodeId))
                throw new ArgumentException("Slug and episodeId are required");

            try
            {
                var episode = await _context.CachedEpisodes
                    .AsNoTracking()
                    .Include(e => e.EpisodeServers)
                    .FirstOrDefaultAsync(e => e.MovieSlug == slug && e.Id == episodeId);

                if (episode == null)
                {
                    var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);
                    if (movieDetail != null)
                    {
                        await CacheMovieAsync(movieDetail);
                        episode = await _context.CachedEpisodes
                            .AsNoTracking()
                            .Include(e => e.EpisodeServers)
                            .FirstOrDefaultAsync(e => e.MovieSlug == slug && e.Id == episodeId);
                    }
                }

                if (episode == null)
                {
                    _logger.LogWarning($"Episode {episodeId} for movie {slug} not found.");
                    return null;
                }

                // Ghi lịch sử xem nếu người dùng đã đăng nhập
                var userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var existingEntry = await _context.UserMovies
                        .FirstOrDefaultAsync(um => um.UserId == userId && um.MovieSlug == slug);

                    if (existingEntry != null)
                    {
                        existingEntry.AddedAt = DateTime.UtcNow;
                        _context.UserMovies.Update(existingEntry);
                    }
                    else
                    {
                        var newEntry = new UserMovie
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = userId,
                            MovieSlug = slug,
                            AddedAt = DateTime.UtcNow
                        };
                        _context.UserMovies.Add(newEntry);
                    }

                    await _context.SaveChangesAsync();
                }

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
                _logger.LogError(ex, $"Error getting episode {episodeId} for movie {slug}");
                throw new Exception($"Failed to retrieve episode {episodeId} for movie {slug}", ex);
            }
        }
        public async Task<EpisodeProgress> GetEpisodeProgressAsync(string userId, string movieSlug, string episodeId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(movieSlug) || string.IsNullOrWhiteSpace(episodeId))
                throw new ArgumentException("UserId, movieSlug, and episodeId are required");

            try
            {
                var progress = await _context.EpisodeProgresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.EpisodeId == episodeId);

                if (progress == null)
                {
                    _logger.LogInformation($"No progress found for user {userId}, movie {movieSlug}, episode {episodeId}");
                }

                return progress;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting episode progress for user {userId}, movie {movieSlug}, episode {episodeId}");
                throw new Exception($"Failed to retrieve episode progress for user {userId}, movie {movieSlug}, episode {episodeId}", ex);
            }
        }

        public async Task UpdateEpisodeProgressAsync(string userId, string movieSlug, string episodeId, double currentTime, double duration)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(movieSlug) || string.IsNullOrWhiteSpace(episodeId))
                throw new ArgumentException("UserId, movieSlug, and episodeId are required");

            if (currentTime < 0 || duration <= 0)
                throw new ArgumentException("Invalid currentTime or duration");

            try
            {
                var progress = await _context.EpisodeProgresses
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.EpisodeId == episodeId);

                double watchedPercentage = duration > 0 ? Math.Min((currentTime / duration) * 100, 100) : 0;

                if (progress == null)
                {
                    progress = new EpisodeProgress
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = userId,
                        EpisodeId = episodeId,
                        WatchedPercentage = watchedPercentage,
                        LastWatched = DateTime.UtcNow
                    };

                    _context.EpisodeProgresses.Add(progress);
                }
                else
                {
                    progress.WatchedPercentage = watchedPercentage;
                    progress.LastWatched = DateTime.UtcNow;

                    _context.EpisodeProgresses.Update(progress);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated episode progress for user {userId}, movie {movieSlug}, episode {episodeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating progress for episode {episodeId} of movie {movieSlug}");
                throw new Exception($"Failed to update episode progress for episode {episodeId} of movie {movieSlug}", ex);
            }
        }

        public async Task IncrementViewCountAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            try
            {
                var movie = await _context.CachedMovies
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (movie != null)
                {
                    movie.Views++; 
                    _context.CachedMovies.Update(movie);
                }

                var statistic = await _context.MovieStatistics
                    .FirstOrDefaultAsync(s => s.MovieSlug == slug);

                if (statistic == null)
                {
                    statistic = new MovieStatistic
                    {
                        MovieSlug = slug,
                        Views = 1,
                        FavoriteCount = 0,
                        AverageRating = 0,
                        LastUpdated = DateTime.UtcNow
                    };

                    _context.MovieStatistics.Add(statistic);
                }
                else
                {
                    statistic.Views++; // Using long
                    statistic.LastUpdated = DateTime.UtcNow;
                    _context.MovieStatistics.Update(statistic);
                }

                // Cập nhật DailyView
                var today = DateTime.UtcNow.Date;
                var dailyView = await _context.DailyViews
                    .FirstOrDefaultAsync(d => d.MovieSlug == slug && d.Date == today);

                if (dailyView == null)
                {
                    dailyView = new DailyView
                    {
                        MovieSlug = slug,
                        Date = today,
                        Views = 1
                    };
                    _context.DailyViews.Add(dailyView);
                }
                else
                {
                    dailyView.Views++;
                    _context.DailyViews.Update(dailyView);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Incremented view count for movie {slug}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing view count for movie {slug}");
                // Non-critical operation, no need to throw
            }
        }

        private async Task ProcessEpisodesAsync(string movieSlug, List<Episode> episodes)
        {
            try
            {
                var existingEpisodes = await _context.CachedEpisodes
                    .Include(e => e.EpisodeServers)
                    .Where(e => e.MovieSlug == movieSlug)
                    .ToListAsync();

                // Xóa các tập phim và server cũ
                foreach (var episode in existingEpisodes)
                {
                    _context.EpisodeServers.RemoveRange(episode.EpisodeServers);
                }
                _context.CachedEpisodes.RemoveRange(existingEpisodes);

                foreach (var episode in episodes)
                {
                    if (episode.Items == null || !episode.Items.Any())
                        continue;

                    foreach (var item in episode.Items)
                    {
                        if (string.IsNullOrEmpty(item.Url))
                            continue;

                        var episodeNumber = int.TryParse(item.Title?.Replace("Episode ", ""), out var num)
                            ? num
                            : episode.Items.IndexOf(item) + 1;

                        var cachedEpisode = new CachedEpisode
                        {
                            Id = item.Id ?? Guid.NewGuid().ToString(),
                            MovieSlug = movieSlug,
                            EpisodeNumber = episodeNumber,
                            Title = item.Title,
                            Url = item.Url,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            EpisodeServers = new List<EpisodeServer>
                            {
                                new EpisodeServer
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    ServerName = episode.ServerName,
                                    ServerUrl = item.Url
                                }
                            }
                        };

                        _context.CachedEpisodes.Add(cachedEpisode);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Processed {episodes.Sum(e => e.Items?.Count ?? 0)} episodes for movie {movieSlug}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing episodes for movie {movieSlug}");
                throw new Exception($"Failed to process episodes for movie {movieSlug}", ex);
            }
        }

        private async Task ProcessAdditionalDataAsync(CachedMovie cachedMovie, MovieDetailResponse movieDetail)
        {
            try
            {
                // Lấy hình ảnh từ endpoint get-img
                var images = await _movieApiService.GetMovieImagesAsync(movieDetail.Slug);
                if (images?.Success == true)
                {
                    cachedMovie.ThumbUrl = images.SubThumb ?? cachedMovie.ThumbUrl;
                    cachedMovie.PosterUrl = images.SubPoster ?? cachedMovie.PosterUrl;
                }

                // Lấy thông tin nhà sản xuất và nền tảng phát trực tuyến
                var productionData = await _movieApiService.GetProductionDataAsync(movieDetail.Slug);
                if (productionData != null)
                {
                    cachedMovie.MovieProductionCompanies.Clear();
                    cachedMovie.MovieStreamingPlatforms.Clear();

                    foreach (var company in productionData.ProductionCompanies ?? new List<ProductionCompanyData>())
                    {
                        var prodCompany = await _context.ProductionCompanies
                            .FirstOrDefaultAsync(pc => pc.Name == company.Name);
                        if (prodCompany == null)
                        {
                            prodCompany = new ProductionCompany { Name = company.Name };
                            _context.ProductionCompanies.Add(prodCompany);
                            await _context.SaveChangesAsync();
                        }

                        cachedMovie.MovieProductionCompanies.Add(new MovieProductionCompany
                        {
                            MovieSlug = cachedMovie.Slug,
                            ProductionCompanyId = prodCompany.Id
                        });
                    }

                    foreach (var platform in productionData.StreamingPlatforms ?? new List<StreamingPlatformData>())
                    {
                        var streamingPlatform = await _context.StreamingPlatforms
                            .FirstOrDefaultAsync(sp => sp.Name == platform.Name);
                        if (streamingPlatform == null)
                        {
                            streamingPlatform = new StreamingPlatform { Name = platform.Name, Url = platform.Logo };
                            _context.StreamingPlatforms.Add(streamingPlatform);
                            await _context.SaveChangesAsync();
                        }

                        cachedMovie.MovieStreamingPlatforms.Add(new MovieStreamingPlatform
                        {
                            MovieSlug = cachedMovie.Slug,
                            StreamingPlatformId = streamingPlatform.Id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing additional data for movie {movieDetail.Slug}");
                // Log but don't throw, as this is non-critical
            }
        }
    }

   
}