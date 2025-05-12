using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMovieApiService _movieApiService;

        public HomeController(IMovieApiService movieApiService)
        {
            _movieApiService = movieApiService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                var movies = await _movieApiService.GetNewMoviesAsync(page);

                if (movies == null || movies.Data == null)
                {
                    return View("Error", new ErrorViewModel { RequestId = "API returned null data" });
                }

                return View(movies);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in Index action: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = ex.Message });
            }
        }

        public async Task<IActionResult> TestApi()
        {
            try
            {
                var movies = await _movieApiService.GetNewMoviesAsync(1, 5);
                return View(movies);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { RequestId = ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}