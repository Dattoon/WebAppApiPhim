using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Controllers
{
    public partial class HomeController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> LoadMoreMovies(int page = 1)
        {
            try
            {
                // Lấy phim mới nhất với số lượng giới hạn
                var latestMovies = await _movieApiService.GetLatestMoviesAsync(page, 8);

                if (latestMovies?.Data == null || !latestMovies.Data.Any())
                {
                    return Content("");
                }

                return PartialView("_MovieGrid", latestMovies.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading more movies for page {page}");
                return Content("<div class='alert alert-danger'>Có lỗi xảy ra khi tải thêm phim. Vui lòng thử lại sau.</div>");
            }
        }
    }
}
