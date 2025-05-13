using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            IUserService userService,
            IMovieApiService movieApiService,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _userService = userService;
            _movieApiService = movieApiService;
            _logger = logger;
        }

        // Danh sách yêu thích
        [HttpGet]
        public async Task<IActionResult> Favorites(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var pageSize = 12;
            var favorites = await _userService.GetFavoritesAsync(user.Id, page, pageSize);
            var totalItems = await _userService.GetFavoritesCountAsync(user.Id);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(favorites);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(string movieSlug, string movieName, string moviePosterUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userService.AddToFavoritesAsync(user.Id, movieSlug, movieName, moviePosterUrl);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = result });
            }

            return RedirectToAction("Detail", "Movie", new { slug = movieSlug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userService.RemoveFromFavoritesAsync(user.Id, movieSlug);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = result });
            }

            return RedirectToAction("Favorites");
        }

        [HttpGet]
        public async Task<IActionResult> IsFavorite(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { isFavorite = false });
            }

            var isFavorite = await _userService.IsFavoriteAsync(user.Id, movieSlug);
            return Json(new { isFavorite });
        }

        // Lịch sử xem
        [HttpGet]
        public async Task<IActionResult> WatchHistory(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var pageSize = 12;
            var history = await _userService.GetWatchHistoryAsync(user.Id, page, pageSize);
            var totalItems = await _userService.GetWatchHistoryCountAsync(user.Id);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWatchHistory(string movieSlug, string movieName, string moviePosterUrl, string episodeSlug = null, string episodeName = null, double watchedPercentage = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userService.AddToWatchHistoryAsync(user.Id, movieSlug, movieName, moviePosterUrl, episodeSlug, episodeName, watchedPercentage);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = result });
            }

            return RedirectToAction("Watch", "Movie", new { slug = movieSlug, episode = episodeSlug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWatchHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userService.ClearWatchHistoryAsync(user.Id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = result });
            }

            return RedirectToAction("WatchHistory");
        }

        [HttpGet]
        public async Task<IActionResult> GetLastWatched(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false });
            }

            var lastWatched = await _userService.GetLastWatchedAsync(user.Id, movieSlug);
            return Json(new { success = true, lastWatched });
        }

        // Bình luận
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string movieSlug, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Nội dung bình luận không được để trống.");
            }

            var comment = await _userService.AddCommentAsync(user.Id, movieSlug, content);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        createdAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        user = new
                        {
                            id = user.Id,
                            displayName = user.DisplayName,
                            avatarUrl = user.AvatarUrl
                        }
                    }
                });
            }

            return RedirectToAction("Detail", "Movie", new { slug = movieSlug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComment(int commentId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Nội dung bình luận không được để trống.");
            }

            var result = await _userService.UpdateCommentAsync(commentId, user.Id, content);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = result });
            }

            return RedirectToAction("Detail", "Movie", new { slug = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userService.DeleteCommentAsync(commentId, user.Id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = result });
            }

            return RedirectToAction("Detail", "Movie", new { slug = "" });
        }

        // Đánh giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateMovie(string movieSlug, int value)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (value < 1 || value > 10)
            {
                return BadRequest("Giá trị đánh giá phải từ 1 đến 10.");
            }

            var result = await _userService.AddOrUpdateRatingAsync(user.Id, movieSlug, value);
            var averageRating = await _userService.GetAverageRatingAsync(movieSlug);
            var ratingCount = await _userService.GetRatingCountAsync(movieSlug);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = result,
                    averageRating = Math.Round(averageRating, 1),
                    ratingCount = ratingCount
                });
            }

            return RedirectToAction("Detail", "Movie", new { slug = movieSlug });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRating(string movieSlug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { hasRating = false });
            }

            var rating = await _userService.GetUserRatingAsync(user.Id, movieSlug);
            return Json(new
            {
                hasRating = rating != null,
                value = rating?.Value ?? 0
            });
        }
    }
}
