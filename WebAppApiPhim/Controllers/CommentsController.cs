using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace YourNamespace.Controllers
{
    [Route("api/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Replace YourDbContext
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
                    UserId = Guid.Parse(userId),
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
        public async Task<ActionResult<IEnumerable<UserComment>>> GetComments(string movieSlug)
        {
            if (string.IsNullOrWhiteSpace(movieSlug))
            {
                _logger.LogWarning("Invalid movieSlug parameter provided.");
                return BadRequest("MovieSlug is required.");
            }

            try
            {
                var comments = await _context.UserComments
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

        // DELETE: api/comments/{commentId}
        [HttpDelete("{commentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteComment(string commentId)
        {
            if (string.IsNullOrWhiteSpace(commentId))
            {
                _logger.LogWarning("Invalid commentId parameter provided.");
                return BadRequest("CommentId is required.");
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var comment = await _context.UserComments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    _logger.LogWarning($"Comment with ID {commentId} not found.");
                    return NotFound($"Comment with ID {commentId} not found.");
                }

                // Check if the user owns this comment
                if (comment.UserId != Guid.Parse(userId))
                {
                    _logger.LogWarning($"User {userId} attempted to delete comment {commentId} they don't own.");
                    return Forbid("You can only delete your own comments.");
                }

                _context.UserComments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Comment {commentId} deleted by user {userId}.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting comment {commentId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the comment.");
            }
        }

        // POST: api/comments/reply
        [HttpPost("reply")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserComment>> ReplyToComment(
            [FromQuery] string parentCommentId,
            [FromQuery] string content)
        {
            if (string.IsNullOrWhiteSpace(parentCommentId) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Invalid parentCommentId or content parameters provided.");
                return BadRequest("ParentCommentId and content are required.");
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Find the parent comment
                var parentComment = await _context.UserComments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == parentCommentId);

                if (parentComment == null)
                {
                    _logger.LogWarning($"Parent comment with ID {parentCommentId} not found.");
                    return NotFound($"Parent comment with ID {parentCommentId} not found.");
                }

                // Create reply comment with special format to indicate it's a reply
                var replyContent = $"@{parentComment.User.UserName}: {content} [REPLY_TO:{parentCommentId}]";

                var replyComment = new UserComment
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = Guid.Parse(userId),
                    MovieSlug = parentComment.MovieSlug,
                    Content = replyContent,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserComments.Add(replyComment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reply comment created for parent comment {parentCommentId} by user {userId}.");
                return CreatedAtAction(nameof(GetComments), new { movieSlug = parentComment.MovieSlug }, replyComment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating reply for comment {parentCommentId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the reply.");
            }
        }

        // GET: api/comments/{movieSlug}/structured
        [HttpGet("{movieSlug}/structured")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CommentWithRepliesViewModel>>> GetCommentsWithReplies(string movieSlug)
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
                    .Include(c => c.User)
                    .Where(c => c.MovieSlug == movieSlug)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                if (!comments.Any())
                {
                    _logger.LogWarning($"No comments found for movie {movieSlug}.");
                    return NotFound($"No comments found for movie {movieSlug}.");
                }

                // Separate main comments and replies
                var mainComments = new List<CommentWithRepliesViewModel>();
                var replyComments = new Dictionary<string, List<UserComment>>();

                foreach (var comment in comments)
                {
                    if (comment.Content.Contains("[REPLY_TO:"))
                    {
                        // This is a reply
                        var replyToMatch = System.Text.RegularExpressions.Regex.Match(comment.Content, @"\[REPLY_TO:([^\]]+)\]");
                        if (replyToMatch.Success)
                        {
                            var parentId = replyToMatch.Groups[1].Value;
                            if (!replyComments.ContainsKey(parentId))
                            {
                                replyComments[parentId] = new List<UserComment>();
                            }
                            replyComments[parentId].Add(comment);
                        }
                    }
                    else
                    {
                        // This is a main comment
                        mainComments.Add(new CommentWithRepliesViewModel
                        {
                            Comment = comment,
                            Replies = new List<UserComment>()
                        });
                    }
                }

                // Attach replies to their parent comments
                foreach (var mainComment in mainComments)
                {
                    if (replyComments.ContainsKey(mainComment.Comment.Id))
                    {
                        mainComment.Replies = replyComments[mainComment.Comment.Id]
                            .OrderBy(r => r.CreatedAt)
                            .ToList();
                    }
                }

                return Ok(mainComments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving structured comments for movie {movieSlug}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving comments.");
            }
        }

        // GET: api/comments/{commentId}/replies
        [HttpGet("{commentId}/replies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<UserComment>>> GetCommentReplies(string commentId)
        {
            if (string.IsNullOrWhiteSpace(commentId))
            {
                _logger.LogWarning("Invalid commentId parameter provided.");
                return BadRequest("CommentId is required.");
            }

            try
            {
                var replies = await _context.UserComments
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Where(c => c.Content.Contains($"[REPLY_TO:{commentId}]"))
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(replies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving replies for comment {commentId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving replies.");
            }
        }

        // PUT: api/comments/{commentId}
        [HttpPut("{commentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserComment>> UpdateComment(
            string commentId,
            [FromQuery] string content)
        {
            if (string.IsNullOrWhiteSpace(commentId) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Invalid commentId or content parameters provided.");
                return BadRequest("CommentId and content are required.");
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var comment = await _context.UserComments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    _logger.LogWarning($"Comment with ID {commentId} not found.");
                    return NotFound($"Comment with ID {commentId} not found.");
                }

                // Check if the user owns this comment
                if (comment.UserId != Guid.Parse(userId))
                {
                    _logger.LogWarning($"User {userId} attempted to update comment {commentId} they don't own.");
                    return Forbid("You can only update your own comments.");
                }

                // Preserve reply information if this is a reply
                if (comment.Content.Contains("[REPLY_TO:"))
                {
                    var replyToMatch = System.Text.RegularExpressions.Regex.Match(comment.Content, @"@[^:]+: (.+) \[REPLY_TO:([^\]]+)\]");
                    if (replyToMatch.Success)
                    {
                        var parentId = replyToMatch.Groups[2].Value;
                        var parentComment = await _context.UserComments
                            .Include(c => c.User)
                            .FirstOrDefaultAsync(c => c.Id == parentId);

                        if (parentComment != null)
                        {
                            comment.Content = $"@{parentComment.User.UserName}: {content} [REPLY_TO:{parentId}]";
                        }
                    }
                }
                else
                {
                    comment.Content = content;
                }

                _context.UserComments.Update(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Comment {commentId} updated by user {userId}.");
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating comment {commentId}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the comment.");
            }
        }
    }

 
}