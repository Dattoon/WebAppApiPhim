using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
            if (movie == null || string.IsNullOrWhiteSpace(movie.Slug))
                throw new ArgumentException("Movie detail or slug is required", nameof(movie));

            try
            {
                var existingMovie = await dbContext.CachedMovies
                    .FirstOrDefaultAsync(m => m.Slug == movie.Slug);

                if (existingMovie == null)
                {
                    var cachedMovie = new CachedMovie
                    {
                        Slug = movie.Slug,
                        Title = movie.Title,
                        Description = movie.Description,
                        PosterUrl = movie.PosterUrl ?? "/placeholder.svg?height=450&width=300",
                        ThumbUrl = movie.ThumbUrl ?? "/placeholder.svg?height=450&width=300",
                        Year = movie.Year,
                        Director = movie.Director,
                        Duration = movie.Duration,
                        Language = movie.Language,
                        Views = movie.View,
                        LastUpdated = DateTime.UtcNow,
                        TmdbId = movie.TmdbId,
                        TrailerUrl = movie.TrailerUrl, // Bổ sung TrailerUrl
                        RawData = JsonSerializer.Serialize(movie, jsonOptions)
                    };
                    dbContext.CachedMovies.Add(cachedMovie);
                }
                else
                {
                    existingMovie.Title = movie.Title;
                    existingMovie.Description = movie.Description;
                    existingMovie.PosterUrl = movie.PosterUrl ?? existingMovie.PosterUrl;
                    existingMovie.ThumbUrl = movie.ThumbUrl ?? existingMovie.ThumbUrl;
                    existingMovie.Year = movie.Year;
                    existingMovie.Director = movie.Director;
                    existingMovie.Duration = movie.Duration;
                    existingMovie.Language = movie.Language;
                    existingMovie.Views = movie.View;
                    existingMovie.LastUpdated = DateTime.UtcNow;
                    existingMovie.TmdbId = movie.TmdbId;
                    existingMovie.TrailerUrl = movie.TrailerUrl ?? existingMovie.TrailerUrl; 
                    existingMovie.RawData = JsonSerializer.Serialize(movie, jsonOptions);

                    dbContext.CachedMovies.Update(existingMovie);
                }

                // Xử lý Genres
                if (movie.Genres?.Any() == true)
                {
                    foreach (var genreName in movie.Genres)
                    {
                        var genre = await dbContext.MovieGenres.FirstOrDefaultAsync(g => g.Name == genreName);
                        if (genre == null)
                        {
                            genre = new MovieGenre { Name = genreName };
                            dbContext.MovieGenres.Add(genre);
                            await dbContext.SaveChangesAsync();
                        }
                        if (!await dbContext.MovieGenreMappings.AnyAsync(mgm => mgm.MovieSlug == movie.Slug && mgm.GenreId == genre.Id))
                        {
                            dbContext.MovieGenreMappings.Add(new MovieGenreMapping
                            {
                                MovieSlug = movie.Slug,
                                GenreId = genre.Id
                            });
                        }
                    }
                }

                // Xử lý Countries
                if (movie.Countries?.Any() == true)
                {
                    foreach (var countryName in movie.Countries)
                    {
                        var country = await dbContext.MovieCountries.FirstOrDefaultAsync(c => c.Name == countryName);
                        if (country == null)
                        {
                            country = new MovieCountry { Name = countryName };
                            dbContext.MovieCountries.Add(country);
                            await dbContext.SaveChangesAsync();
                        }
                        if (!await dbContext.MovieCountryMappings.AnyAsync(mcm => mcm.MovieSlug == movie.Slug && mcm.CountryId == country.Id))
                        {
                            dbContext.MovieCountryMappings.Add(new MovieCountryMapping
                            {
                                MovieSlug = movie.Slug,
                                CountryId = country.Id
                            });
                        }
                    }
                }

                // Xử lý Type
                if (!string.IsNullOrWhiteSpace(movie.Type))
                {
                    var movieType = await dbContext.MovieTypes.FirstOrDefaultAsync(t => t.Name == movie.Type);
                    if (movieType == null)
                    {
                        movieType = new MovieType { Name = movie.Type };
                        dbContext.MovieTypes.Add(movieType);
                        await dbContext.SaveChangesAsync();
                    }
                    if (!await dbContext.MovieTypeMappings.AnyAsync(mtm => mtm.MovieSlug == movie.Slug && mtm.TypeId == movieType.Id))
                    {
                        dbContext.MovieTypeMappings.Add(new MovieTypeMapping
                        {
                            MovieSlug = movie.Slug,
                            TypeId = movieType.Id
                        });
                    }
                }

                // Xử lý Actors
                if (!string.IsNullOrWhiteSpace(movie.Actors))
                {
                    var actorNames = movie.Actors.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a));
                    foreach (var actorName in actorNames)
                    {
                        var actor = await dbContext.Actors.FirstOrDefaultAsync(a => a.Name == actorName);
                        if (actor == null)
                        {
                            actor = new Actor { Name = actorName };
                            dbContext.Actors.Add(actor);
                            await dbContext.SaveChangesAsync();
                        }
                        if (!await dbContext.MovieActors.AnyAsync(ma => ma.MovieSlug == movie.Slug && ma.ActorId == actor.Id))
                        {
                            dbContext.MovieActors.Add(new MovieActor
                            {
                                MovieSlug = movie.Slug,
                                ActorId = actor.Id
                            });
                        }
                    }
                }

                // Xử lý tập phim
                if (movie.Episodes?.Any() == true)
                {
                    var existingEpisodes = await dbContext.CachedEpisodes
                        .Where(e => e.MovieSlug == movie.Slug)
                        .ToListAsync();
                    dbContext.CachedEpisodes.RemoveRange(existingEpisodes);

                    foreach (var episode in movie.Episodes)
                    {
                        if (episode.Items == null || !episode.Items.Any()) continue;

                        foreach (var item in episode.Items)
                        {
                            var episodeNumber = int.TryParse(item.Title?.Replace("Episode ", ""), out var num)
                                ? num
                                : episode.Items.IndexOf(item) + 1;

                            var cachedEpisode = new CachedEpisode
                            {
                                Id = item.Id ?? Guid.NewGuid().ToString(),
                                MovieSlug = movie.Slug,
                                EpisodeNumber = episodeNumber,
                                Title = item.Title ?? episode.EpisodeName,
                                Url = item.Url ?? episode.M3u8Url ?? episode.EmbedUrl,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                EpisodeServers = new List<EpisodeServer>
                                {
                                    new EpisodeServer
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        EpisodeId = item.Id ?? Guid.NewGuid().ToString(),
                                        ServerName = episode.ServerName,
                                        ServerUrl = item.Url ?? episode.M3u8Url ?? episode.EmbedUrl
                                    }
                                }
                            };
                            dbContext.CachedEpisodes.Add(cachedEpisode);
                        }
                    }
                }

                // Cập nhật thống kê (bao gồm Rating)
                var statistic = await dbContext.MovieStatistics
                    .FirstOrDefaultAsync(s => s.MovieSlug == movie.Slug);

                if (statistic == null)
                {
                    dbContext.MovieStatistics.Add(new MovieStatistic
                    {
                        MovieSlug = movie.Slug,
                        Views = (int)movie.View,
                        AverageRating = movie.Rating ?? 0, // Bổ sung Rating
                        FavoriteCount = 0,
                        LastUpdated = DateTime.UtcNow
                    });
                }
                else
                {
                    statistic.Views = (int)movie.View;
                    statistic.AverageRating = movie.Rating ?? statistic.AverageRating; // Cập nhật Rating
                    statistic.LastUpdated = DateTime.UtcNow;
                    dbContext.MovieStatistics.Update(statistic);
                }

                // Xử lý ProductionCompanies và StreamingPlatforms (giả định lấy từ ProductionApiResponse)
                // Lưu ý: Cần một dịch vụ để gọi API production, ở đây chỉ thêm placeholder
                // var productionData = await GetProductionDataAsync(movie.Slug); // Giả định phương thức
                // if (productionData != null)
                // {
                //     foreach (var company in productionData.ProductionCompanies)
                //     {
                //         var prodCompany = await dbContext.ProductionCompanies.FirstOrDefaultAsync(pc => pc.Name == company.Name);
                //         if (prodCompany == null)
                //         {
                //             prodCompany = new ProductionCompany { Name = company.Name };
                //             dbContext.ProductionCompanies.Add(prodCompany);
                //             await dbContext.SaveChangesAsync();
                //         }
                //         dbContext.MovieProductionCompanies.Add(new MovieProductionCompany
                //         {
                //             MovieSlug = movie.Slug,
                //             ProductionCompanyId = prodCompany.Id
                //         });
                //     }
                //     foreach (var platform in productionData.StreamingPlatforms)
                //     {
                //         var streamPlatform = await dbContext.StreamingPlatforms.FirstOrDefaultAsync(sp => sp.Name == platform.Name);
                //         if (streamPlatform == null)
                //         {
                //             streamPlatform = new StreamingPlatform { Name = platform.Name, Url = platform.Logo }; // Giả định Url từ Logo
                //             dbContext.StreamingPlatforms.Add(streamPlatform);
                //             await dbContext.SaveChangesAsync();
                //         }
                //         dbContext.MovieStreamingPlatforms.Add(new MovieStreamingPlatform
                //         {
                //             MovieSlug = movie.Slug,
                //             StreamingPlatformId = streamPlatform.Id
                //         });
                //     }
                // }

                await dbContext.SaveChangesAsync();
                logger.LogInformation($"Successfully stored movie {movie.Slug} in database");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error storing movie {movie.Slug} in database");
                throw new Exception($"Failed to store movie with slug {movie.Slug}", ex);
            }
        }

        // Placeholder cho phương thức lấy dữ liệu sản xuất (cần triển khai nếu có API)
        // private static async Task<ProductionApiData> GetProductionDataAsync(string slug)
        // {
        //     // Gọi API hoặc logic lấy dữ liệu production
        //     return null; // Placeholder
        // }
    }
}