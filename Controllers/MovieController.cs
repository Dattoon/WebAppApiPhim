using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieApiService _movieApiService;
        private readonly IUserService _userService;

        public MovieController(IMovieApiService movieApiService, IUserService userService)
        {
            _movieApiService = movieApiService;
            _userService = userService;
        }

        public async Task<IActionResult> Detail(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return RedirectToAction("Index", "Home");
            }

            var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);

            if (movieDetail?.Movie == null)
            {
                return View(new MovieDetailViewModel());
            }

            var viewModel = new MovieDetailViewModel
            {
                Movie = movieDetail.Movie,
                Episodes = movieDetail.Episodes?.OfType<Episode>().ToList() ?? new List<Episode>(),
                RelatedMovies = (await _movieApiService.GetRelatedMoviesAsync(slug))?.Data ?? new List<MovieItem>()
            };

            // Get user-specific data if logged in
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                viewModel.IsFavorite = await _userService.IsFavoriteAsync(userId, slug);
                viewModel.WatchHistory = await _userService.GetMovieWatchHistoryAsync(userId, slug);
            }

            // Get comments
            viewModel.Comments = await _userService.GetMovieCommentsAsync(slug);

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWatchProgress(string slug, string name, double percentage)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _userService.UpdateWatchProgressAsync(userId, slug, name, percentage);
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(string slug, string name)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var favorite = await _userService.ToggleFavoriteAsync(userId, slug, name);
            return Json(new { success = true, isFavorite = favorite != null });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string slug, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return RedirectToAction("Detail", new { slug });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _userService.AddCommentAsync(userId, slug, content);
            return RedirectToAction("Detail", new { slug });
        }

        public async Task<IActionResult> Watch(string slug, string episode)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return RedirectToAction("Index", "Home");
            }

            var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);

            if (movieDetail?.Movie == null)
            {
                return RedirectToAction("Detail", new { slug });
            }

            // Track watch progress if user is logged in
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _userService.UpdateWatchProgressAsync(userId, slug, movieDetail.Movie.Name, 0);
            }

            return View(movieDetail);
        }
    }
}