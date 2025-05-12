using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Favorites()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var favorites = await _userService.GetUserFavoritesAsync(userId);
            return View(favorites);
        }

        public async Task<IActionResult> WatchHistory()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var history = await _userService.GetUserWatchHistoryAsync(userId);
            return View(history);
        }
    }
}