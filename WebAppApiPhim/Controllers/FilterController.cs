using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FilterController> _logger;

        public FilterController(ApplicationDbContext context, ILogger<FilterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Advanced movie filtering
        /// </summary>
        [HttpGet("movies")]
        public async Task<ActionResult<MovieListResponse>> FilterMovies(
            [FromQuery] string? genre = null,
            [FromQuery] string? country = null,
            [FromQuery] string? year = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "latest", // latest, title, year, views
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var query = _context.CachedMovies.AsNoTracking().AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    var genreEntity = await _context.MovieGenres
                        .FirstOrDefaultAsync(g => g.Slug == genre);

                    if (genreEntity != null)
                    {
                        var movieSlugsWithGenre = await _context.MovieGenreMappings
                            .Where(mgm => mgm.GenreId == genreEntity.Id)
                            .Select(mgm => mgm.MovieSlug)
                            .ToListAsync();

                        query = query.Where(m => movieSlugsWithGenre.Contains(m.Slug));
                    }
                }

                if (!string.IsNullOrWhiteSpace(country))
                {
                    var countryEntity = await _context.MovieCountries
                        .FirstOrDefaultAsync(c => c.Slug == country);

                    if (countryEntity != null)
                    {
                        var movieSlugsWithCountry = await _context.MovieCountryMappings
                            .Where(mcm => mcm.CountryId == countryEntity.Id)
                            .Select(mcm => mcm.MovieSlug)
                            .ToListAsync();

                        query = query.Where(m => movieSlugsWithCountry.Contains(m.Slug));
                    }
                }

                if (!string.IsNullOrWhiteSpace(year))
                {
                    query = query.Where(m => m.Year == year);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    query = query.Where(m => m.Title.ToLower().Contains(searchTerm) ||
                                           m.Description.ToLower().Contains(searchTerm));
                }

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "title" => query.OrderBy(m => m.Title),
                    "year" => query.OrderByDescending(m => m.Year),
                    "views" => query.OrderByDescending(m => m.Views),
                    _ => query.OrderByDescending(m => m.LastUpdated)
                };

                var totalItems = await query.CountAsync();
                var movies = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var response = new MovieListResponse
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
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering movies");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get filter options
        /// </summary>
        [HttpGet("options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var genres = await _context.MovieGenres
                    .Where(g => _context.MovieGenreMappings.Any(mgm => mgm.GenreId == g.Id))
                    .Select(g => new { g.Name, g.Slug })
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                var countries = await _context.MovieCountries
                    .Where(c => _context.MovieCountryMappings.Any(mcm => mcm.CountryId == c.Id))
                    .Select(c => new { c.Name, c.Slug })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var years = await _context.CachedMovies
                    .Where(m => !string.IsNullOrEmpty(m.Year))
                    .Select(m => m.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync();

                return Ok(new
                {
                    genres = genres,
                    countries = countries,
                    years = years,
                    sortOptions = new[]
                    {
                        new { value = "latest", label = "Mới nhất" },
                        new { value = "title", label = "Tên phim" },
                        new { value = "year", label = "Năm sản xuất" },
                        new { value = "views", label = "Lượt xem" }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
