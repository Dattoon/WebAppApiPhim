using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Helpers
{
    public static class MovieDatabaseHelper
    {
        public static async Task StoreMovieInDatabaseAsync(
            MovieDetailResponse movie,
            ApplicationDbContext dbContext,
            ILogger logger,
            JsonSerializerOptions jsonOptions)
        {
            var existingMovie = await dbContext.CachedMovies
                .FirstOrDefaultAsync(m => m.Slug == movie.Slug);

            if (existingMovie == null)
            {
                var cachedMovie = new CachedMovie
                {
                    Slug = movie.Slug,
                    Name = movie.Name,
                    OriginalName = movie.OriginalName ?? movie.Name,
                    Description = movie.Description,
                    PosterUrl = movie.Poster_url ?? movie.Sub_poster,
                    ThumbUrl = movie.Thumb_url ?? movie.Sub_thumb,
                    Year = movie.Year,
                    Type = movie.Format,
                    Country = movie.Countries,
                    Genres = movie.Genres,
                    Director = movie.Director ?? movie.Directors,
                    Actors = movie.Casts ?? movie.Actors,
                    Duration = movie.Time,
                    Quality = movie.Quality,
                    Language = movie.Language,
                    ViewCount = movie.View,
                    LastUpdated = DateTime.Now,
                    RawData = JsonSerializer.Serialize(movie, jsonOptions)
                };
                dbContext.CachedMovies.Add(cachedMovie);
            }
            else
            {
                existingMovie.Name = movie.Name;
                existingMovie.OriginalName = movie.OriginalName ?? movie.Name;
                existingMovie.Description = movie.Description;
                existingMovie.PosterUrl = movie.Poster_url ?? movie.Sub_poster ?? existingMovie.PosterUrl;
                existingMovie.ThumbUrl = movie.Thumb_url ?? movie.Sub_thumb ?? existingMovie.ThumbUrl;
                existingMovie.Year = movie.Year;
                existingMovie.Type = movie.Format;
                existingMovie.Country = movie.Countries;
                existingMovie.Genres = movie.Genres;
                existingMovie.Director = movie.Director ?? movie.Directors;
                existingMovie.Actors = movie.Casts ?? movie.Actors;
                existingMovie.Duration = movie.Time;
                existingMovie.Quality = movie.Quality;
                existingMovie.Language = movie.Language;
                existingMovie.LastUpdated = DateTime.Now;
                existingMovie.RawData = JsonSerializer.Serialize(movie, jsonOptions);

                dbContext.CachedMovies.Update(existingMovie);
            }

            // Remove old episodes
            if (movie.Episodes?.Any() == true)
            {
                var oldEpisodes = await dbContext.CachedEpisodes
                    .Where(e => e.MovieSlug == movie.Slug)
                    .ToListAsync();
                dbContext.CachedEpisodes.RemoveRange(oldEpisodes);

                foreach (var server in movie.Episodes)
                {
                    try
                    {
                        var serverObj = JsonSerializer.Deserialize<JsonElement>(
                            JsonSerializer.Serialize(server));

                        var serverName = serverObj.GetProperty("server_name").GetString();
                        var items = serverObj.GetProperty("items").EnumerateArray();

                        foreach (var item in items)
                        {
                            dbContext.CachedEpisodes.Add(new CachedEpisode
                            {
                                MovieSlug = movie.Slug,
                                ServerName = serverName,
                                EpisodeName = item.GetProperty("name").GetString(),
                                EpisodeSlug = item.GetProperty("slug").GetString(),
                                EmbedUrl = item.GetProperty("embed").GetString(),
                                M3u8Url = item.GetProperty("m3u8").GetString(),
                                LastUpdated = DateTime.Now
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error processing episode data for {movie.Slug}");
                    }
                }
            }

            // Statistics
            var statistic = await dbContext.MovieStatistics
                .FirstOrDefaultAsync(s => s.MovieSlug == movie.Slug);

            if (statistic == null)
            {
                dbContext.MovieStatistics.Add(new MovieStatistic
                {
                    MovieSlug = movie.Slug,
                    ViewCount = movie.View,
                    FavoriteCount = 0,
                    CommentCount = 0,
                    AverageRating = 0,
                    RatingCount = 0,
                    LastUpdated = DateTime.Now
                });
            }
            else
            {
                statistic.ViewCount = Math.Max(movie.View, statistic.ViewCount);
                statistic.LastUpdated = DateTime.Now;
                dbContext.MovieStatistics.Update(statistic);
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
