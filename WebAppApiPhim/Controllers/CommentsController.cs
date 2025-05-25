using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using System.Threading.Tasks;
using System;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(ApplicationDbContext context, ILogger<CommentsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // POST: api/comments
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserComment>> AddComment(
            [FromQuery] string movieSlug,
            [FromQuery] string content)
        {
            if (string.IsNullOrWhiteSpace(movieSlug) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Invalid movieSlug or content parameters provided.");
                return BadRequest("MovieSlug and content are required.");
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var movieExists = await _context.CachedMovies.AnyAsync(m => m.Slug == movieSlug);
                if (!movieExists)
                {
                    _logger.LogWarning($"Movie with slug {movieSlug} not found.");
                    return NotFound($"Movie with slug {movieSlug} not found.");
                }

                var comment = new UserComment
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    MovieSlug = movieSlug,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserComments.Add(comment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetComments), new { movieSlug }, comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment for movie with slug {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the comment.");
            }
        }

        // GET: api/comments/{movieSlug}
        [HttpGet("{movieSlug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<UserComment>>> GetComments(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var comments = await _context.UserComments
                    .AsNoTracking()
                    .Where(c => c.MovieSlug == movieSlug)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                if (!comments.Any())
                {
                    _logger.LogWarning($"No comments found for movie {movieSlug}.");
                    return NotFound($"No comments found for movie {movieSlug}.");
                }

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving comments for movie {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving comments.");
            }
        }
    }
}