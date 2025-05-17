using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public UserController(
            UserManager<ApplicationUser> userManager,
            IUserService userService)
        {
            _userManager = userManager;
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl,
                createdAt = user.CreatedAt,
                lastLoginAt = user.LastLoginAt
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] Models.ViewModels.ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userService.UpdateUserProfileAsync(user.Id, model.DisplayName, model.AvatarUrl);
            if (!result.Succeeded)
                return BadRequest(new { message = result.Errors.First().Description });

            return Ok(new
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                displayName = model.DisplayName,
                avatarUrl = model.AvatarUrl ?? user.AvatarUrl
            });
        }

        // Favorites
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var favorites = await _userService.GetFavoritesAsync(user.Id, page, pageSize);
            var totalItems = await _userService.GetFavoritesCountAsync(user.Id);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return Ok(new
            {
                items = favorites,
                pagination = new
                {
                    currentPage = page,
                    totalPages = totalPages,
                    totalItems = totalItems,
                    pageSize = pageSize
                }
            });
        }

        [HttpPost("favorites")]
        public async Task<IActionResult> AddToFavorites([FromBody] FavoriteRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userService.AddToFavoritesAsync(
                user.Id,
                request.MovieSlug,
                request.MovieName,
                request.MoviePosterUrl);

            return Ok(new { success = result });
        }

        [HttpDelete("favorites/{movieSlug}")]
        public async Task<IActionResult> RemoveFromFavorites(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userService.RemoveFromFavoritesAsync(user.Id, movieSlug);
            return Ok(new { success = result });
        }

        [HttpGet("favorites/check/{movieSlug}")]
        public async Task<IActionResult> CheckFavorite(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var isFavorite = await _userService.IsFavoriteAsync(user.Id, movieSlug);
            return Ok(new { isFavorite });
        }

        // Watch History
        [HttpGet("watch-history")]
        public async Task<IActionResult> GetWatchHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var history = await _userService.GetWatchHistoryAsync(user.Id, page, pageSize);
            var totalItems = await _userService.GetWatchHistoryCountAsync(user.Id);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return Ok(new
            {
                items = history,
                pagination = new
                {
                    currentPage = page,
                    totalPages = totalPages,
                    totalItems = totalItems,
                    pageSize = pageSize
                }
            });
        }

        [HttpPost("watch-history")]
        public async Task<IActionResult> AddToWatchHistory([FromBody] WatchHistoryRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userService.AddToWatchHistoryAsync(
                user.Id,
                request.MovieSlug,
                request.MovieName,
                request.MoviePosterUrl,
                request.EpisodeSlug,
                request.EpisodeName,
                request.WatchedPercentage);

            return Ok(new { success = result });
        }

        [HttpDelete("watch-history")]
        public async Task<IActionResult> ClearWatchHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userService.ClearWatchHistoryAsync(user.Id);
            return Ok(new { success = result });
        }

        [HttpGet("watch-history/{movieSlug}")]
        public async Task<IActionResult> GetLastWatched(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var lastWatched = await _userService.GetLastWatchedAsync(user.Id, movieSlug);
            return Ok(new { lastWatched });
        }

        // Comments
        [HttpPost("comments")]
        public async Task<IActionResult> AddComment([FromBody] CommentRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { message = "Comment content cannot be empty" });

            var comment = await _userService.AddCommentAsync(user.Id, request.MovieSlug, request.Content);

            return Ok(new
            {
                id = comment.Id,
                content = comment.Content,
                createdAt = comment.CreatedAt,
                user = new
                {
                    id = user.Id,
                    displayName = user.DisplayName,
                    avatarUrl = user.AvatarUrl
                }
            });
        }

        [HttpPut("comments/{commentId}")]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] CommentUpdateRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { message = "Comment content cannot be empty" });

            var result = await _userService.UpdateCommentAsync(commentId, user.Id, request.Content);
            if (!result)
                return NotFound(new { message = "Comment not found or you don't have permission to update it" });

            return Ok(new { success = true });
        }

        [HttpDelete("comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userService.DeleteCommentAsync(commentId, user.Id);
            if (!result)
                return NotFound(new { message = "Comment not found or you don't have permission to delete it" });

            return Ok(new { success = true });
        }

        // Ratings
        [HttpPost("ratings")]
        public async Task<IActionResult> RateMovie([FromBody] RatingRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            if (request.Value < 1 || request.Value > 10)
                return BadRequest(new { message = "Rating value must be between 1 and 10" });

            var result = await _userService.AddOrUpdateRatingAsync(user.Id, request.MovieSlug, request.Value);
            var averageRating = await _userService.GetAverageRatingAsync(request.MovieSlug);
            var ratingCount = await _userService.GetRatingCountAsync(request.MovieSlug);

            return Ok(new
            {
                success = result,
                averageRating = Math.Round(averageRating, 1),
                ratingCount = ratingCount
            });
        }

        [HttpGet("ratings/{movieSlug}")]
        public async Task<IActionResult> GetUserRating(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var rating = await _userService.GetUserRatingAsync(user.Id, movieSlug);
            return Ok(new
            {
                hasRating = rating != null,
                value = rating?.Value ?? 0
            });
        }
    }

    public class FavoriteRequest
    {
        public string MovieSlug { get; set; }
        public string MovieName { get; set; }
        public string MoviePosterUrl { get; set; }
    }

    public class WatchHistoryRequest
    {
        public string MovieSlug { get; set; }
        public string MovieName { get; set; }
        public string MoviePosterUrl { get; set; }
        public string EpisodeSlug { get; set; }
        public string EpisodeName { get; set; }
        public double WatchedPercentage { get; set; }
    }

    public class CommentRequest
    {
        public string MovieSlug { get; set; }
        public string Content { get; set; }
    }

    public class CommentUpdateRequest
    {
        public string Content { get; set; }
    }

    public class RatingRequest
    {
        public string MovieSlug { get; set; }
        public int Value { get; set; }
    }
}
