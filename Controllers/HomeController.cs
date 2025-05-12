using System.Diagnostics;
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

        // Action hiện tại
        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                var movies = await _movieApiService.GetNewMoviesAsync(page);
                Debug.WriteLine($"Movies data count: {movies?.Data?.Count ?? 0}");
                if (movies?.Data?.FirstOrDefault() != null)
                {
                    var firstMovie = movies.Data.First();
                    Debug.WriteLine($"First movie: {firstMovie.Name}, Slug: {firstMovie.Slug}, Poster: {firstMovie.PosterUrl}");
                }

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

        // Action mới để test API
        public async Task<IActionResult> TestApi(int page = 1, int limit = 10)
        {
            try
            {
                // Gọi API để lấy danh sách phim
                var movies = await _movieApiService.GetNewMoviesAsync(page, limit);

                // Log thông tin để debug
                Debug.WriteLine($"Test API - Page: {page}, Limit: {limit}");
                Debug.WriteLine($"Movies data count: {movies?.Data?.Count ?? 0}");
                Debug.WriteLine($"Pagination: Total pages = {movies?.Pagination?.Total_pages ?? 0}, Current page = {movies?.Pagination?.Current_page ?? 0}");
                if (movies?.Data?.Any() == true)
                {
                    foreach (var movie in movies.Data.Take(5)) // In tối đa 5 phim để kiểm tra
                    {
                        Debug.WriteLine($"Movie: {movie.Name}, Slug: {movie.Slug}, Poster: {movie.PosterUrl}");
                    }
                }
                else
                {
                    Debug.WriteLine("No movies found in the response.");
                }

                // Trả về view với dữ liệu hoặc thông báo
                ViewBag.Message = $"API Test - Page: {page}, Limit: {limit}<br>" +
                                 $"Total movies: {movies?.Data?.Count ?? 0}<br>" +
                                 (movies?.Data?.Any() == true ? "First few movies:<br>" + string.Join("<br>", movies.Data.Take(5).Select(m => $"{m.Name} ({m.Slug})")) : "No movies found.");
                return View(movies);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in TestApi action: {ex.Message}");
                ViewBag.Message = $"Error: {ex.Message}";
                return View(new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() });
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