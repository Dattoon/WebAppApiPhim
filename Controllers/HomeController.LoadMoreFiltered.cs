using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Controllers
{
    public partial class HomeController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> LoadMoreFilteredMovies(string type, string genre, string country, string year, int page = 1)
        {
            try
            {
                // Lấy phim theo bộ lọc với số lượng giới hạn
                var filteredMovies = await _movieApiService.FilterMoviesAsync(type, genre, country, year, page, 10);

                if (filteredMovies?.Data == null || !filteredMovies.Data.Any())
                {
                    return Content("");
                }

                return PartialView("_MovieGrid", filteredMovies.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading more filtered movies for page {page}");
                return Content("<div class='alert alert-danger'>Có lỗi xảy ra khi tải thêm phim. Vui lòng thử lại sau.</div>");
            }
        }
    }
}
