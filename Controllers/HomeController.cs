using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    public partial class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMovieApiService _movieApiService;

        public HomeController(ILogger<HomeController> logger, IMovieApiService movieApiService)
        {
            _logger = logger;
            _movieApiService = movieApiService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                // Giảm số lượng phim tải về mỗi lần để tăng tốc độ
                var latestMovies = await _movieApiService.GetLatestMoviesAsync(page, 8);

                // Tạo view model
                var viewModel = new HomeViewModel
                {
                    LatestMovies = latestMovies?.Data ?? new System.Collections.Generic.List<MovieItem>(),
                    Pagination = latestMovies?.Pagination
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public async Task<IActionResult> Search(string query, int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return RedirectToAction("Index");
                }

                // Giảm số lượng phim tải về mỗi lần để tăng tốc độ
                var searchResults = await _movieApiService.SearchMoviesAsync(query, page, 10);

                var viewModel = new SearchViewModel
                {
                    Query = query,
                    Movies = searchResults?.Data ?? new System.Collections.Generic.List<MovieItem>(),
                    Pagination = searchResults?.Pagination,
                    CurrentPage = page
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching for '{query}'");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public async Task<IActionResult> Filter(string type, string genre, string country, string year, int page = 1)
        {
            try
            {
                // Giảm số lượng phim tải về mỗi lần để tăng tốc độ
                var filterResults = await _movieApiService.FilterMoviesAsync(type, genre, country, year, page, 10);

                var viewModel = new FilterViewModel
                {
                    Type = type,
                    Genre = genre,
                    Country = country,
                    Year = year,
                    Movies = filterResults?.Data ?? new System.Collections.Generic.List<MovieItem>(),
                    Pagination = filterResults?.Pagination,
                    CurrentPage = page
                };

                // Lấy danh sách các bộ lọc để hiển thị trong view
                viewModel.Genres = await _movieApiService.GetGenresAsync();
                viewModel.Countries = await _movieApiService.GetCountriesAsync();
                viewModel.Years = await _movieApiService.GetYearsAsync();
                viewModel.MovieTypes = await _movieApiService.GetMovieTypesAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering movies");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
