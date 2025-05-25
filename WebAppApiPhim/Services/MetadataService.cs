using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
  

    public class MetadataService : IMetadataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MetadataService> _logger;

        public MetadataService(ApplicationDbContext context, ILogger<MetadataService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> AddCountryAsync(string name, string code, string slug)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddGenreAsync(string name, string slug)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddMovieTypeAsync(string name, string slug)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetMetadataAsync(string key)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateMetadataAsync(MovieDetailResponse movie, string slug)
        {
            try
            {
                var cachedMovie = await _context.CachedMovies
                    .FirstOrDefaultAsync(m => m.Slug == slug);
                if (cachedMovie == null)
                {
                    _logger.LogWarning($"Movie with slug {slug} not found in cache.");
                    return;
                }

                // Update genres
                var genres = movie.Genres ?? new List<string>();
                foreach (var genreName in genres)
                {
                    var genre = await _context.MovieGenres
                        .FirstOrDefaultAsync(g => g.Name == genreName);
                    if (genre == null)
                    {
                        genre = new MovieGenre { Name = genreName };
                        _context.MovieGenres.Add(genre);
                    }
                    if (!await _context.MovieGenreMappings.AnyAsync(m => m.MovieSlug == slug && m.GenreId == genre.Id))
                    {
                        _context.MovieGenreMappings.Add(new MovieGenreMapping
                        {
                            MovieSlug = slug,
                            GenreId = genre.Id
                        });
                    }
                }

                // Update countries
                var countries = movie.Countries ?? new List<string>();
                foreach (var countryName in countries)
                {
                    var country = await _context.MovieCountries
                        .FirstOrDefaultAsync(c => c.Name == countryName);
                    if (country == null)
                    {
                        country = new MovieCountry { Name = countryName };
                        _context.MovieCountries.Add(country);
                    }
                    if (!await _context.MovieCountryMappings.AnyAsync(m => m.MovieSlug == slug && m.CountryId == country.Id))
                    {
                        _context.MovieCountryMappings.Add(new MovieCountryMapping
                        {
                            MovieSlug = slug,
                            CountryId = country.Id
                        });
                    }
                }

                // Update types
                var typeName = movie.Type ?? "Unknown";
                var movieType = await _context.MovieTypes
                    .FirstOrDefaultAsync(t => t.Name == typeName);
                if (movieType == null)
                {
                    movieType = new MovieType { Name = typeName };
                    _context.MovieTypes.Add(movieType);
                }
                if (!await _context.MovieTypeMappings.AnyAsync(m => m.MovieSlug == slug && m.TypeId == movieType.Id))
                {
                    _context.MovieTypeMappings.Add(new MovieTypeMapping
                    {
                        MovieSlug = slug,
                        TypeId = movieType.Id
                    });
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated metadata for movie with slug {slug}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating metadata for movie with slug {slug}");
                throw;
            }
        }
    }
}