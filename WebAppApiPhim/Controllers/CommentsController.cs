using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserComment>> AddComment(
            [FromQuery] string movieSlug,
            [FromQuery] string content)
        {
            _logger.LogInformation($"AddComment called with movieSlug: {movieSlug}, content: {content}");

            if (string.IsNullOrWhiteSpace(movieSlug) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Invalid movieSlug or content parameters provided.");
                return BadRequest("MovieSlug and content are required.");
            }

            try
            {
                // Debug: Log all claims
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    return Unauthorized("User not authenticated.");
                }

                _logger.LogInformation($"User ID from token: {userId}");

                // Check if movie exists
                var movieExists = await _context.CachedMovies.AnyAsync(m => m.Slug == movieSlug);
                if (!movieExists)
                {
                    _logger.LogWarning($"Movie with slug {movieSlug} not found.");
                    return NotFound($"Movie with slug {movieSlug} not found.");
                }

                var comment = new UserComment
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = Guid.Parse(userId),
                    MovieSlug = movieSlug,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserComments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Comment added successfully for movie {movieSlug} by user {userId}");
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
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<UserComment>>> GetComments(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var comments = await _context.UserComments
                    .Where(c => c.MovieSlug == movieSlug)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

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
