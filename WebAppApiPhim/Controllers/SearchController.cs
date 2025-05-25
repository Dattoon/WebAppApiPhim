using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using System.Threading.Tasks;
using System.Linq;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ApplicationDbContext context, ILogger<SearchController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/search?query={keyword}&genre={genre}&country={country}&page={page}&limit={limit}
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MovieListResponse>> SearchMovies(
            [FromQuery] string? query,
            [FromQuery] string? genre,
            [FromQuery] string? country,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            if (page < 1 || limit < 1)
            {
                _logger.LogWarning("Invalid page or limit parameters provided.");
                return BadRequest("Page and limit must be positive integers.");
            }

            try
            {
                var movieQuery = _context.CachedMovies
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    query = query.ToLower();
                    movieQuery = movieQuery.Where(m => m.Title.ToLower().Contains(query) || m.Description.ToLower().Contains(query));
                }

                if (!string.IsNullOrWhiteSpace(genre))
                {
                    movieQuery = movieQuery
                        .Include(m => m.MovieGenreMappings)
                        .ThenInclude(mgm => mgm.Genre)
                        .Where(m => m.MovieGenreMappings.Any(mgm => mgm.Genre.Name == genre));
                }

                if (!string.IsNullOrWhiteSpace(country))
                {
                    movieQuery = movieQuery
                        .Include(m => m.MovieCountryMappings)
                        .ThenInclude(mcm => mcm.Country)
                        .Where(m => m.MovieCountryMappings.Any(mcm => mcm.Country.Name == country));
                }

                var totalItems = await movieQuery.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / limit);

                var movies = await movieQuery
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
                        Modified = new ModifiedData { Time = m.LastUpdated.ToString("yyyy-MM-dd") }
                    }).ToList(),
                    Pagination = new Pagination
                    {
                        CurrentPage = page,
                        TotalPages = totalPages,
                        TotalItems = totalItems,
                        Limit = limit
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching movies");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching movies.");
            }
        }
    }
}