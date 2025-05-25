// ActorsController.cs
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
    public class ActorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActorsController> _logger;

        public ActorsController(ApplicationDbContext context, ILogger<ActorsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Actor>>> GetActors()
        {
            try
            {
                var actors = await _context.Actors
                    .AsNoTracking()
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                return Ok(actors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving actors");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving actors.");
            }
        }

        [HttpGet("{actorId}/movies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CachedMovie>>> GetMoviesByActor(int actorId)
        {
            try
            {
                var actorExists = await _context.Actors.AnyAsync(a => a.Id == actorId);
                if (!actorExists)
                {
                    _logger.LogWarning($"Actor with ID {actorId} not found.");
                    return NotFound($"Actor with ID {actorId} not found.");
                }

                var movies = await _context.CachedMovies
                    .AsNoTracking()
                    .Include(m => m.MovieActors)
                    .Where(m => m.MovieActors.Any(ma => ma.ActorId == actorId))
                    .ToListAsync();

                return Ok(movies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving movies for actor {actorId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving movies for the actor.");
            }
        }
    }
}