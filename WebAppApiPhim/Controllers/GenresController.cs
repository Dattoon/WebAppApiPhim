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
    public class GenresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GenresController> _logger;

        public GenresController(ApplicationDbContext context, ILogger<GenresController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MovieGenre>>> GetGenres()
        {
            try
            {
                var genres = await _context.MovieGenres
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                return Ok(genres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genres");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving genres.");
            }
        }
    }
}