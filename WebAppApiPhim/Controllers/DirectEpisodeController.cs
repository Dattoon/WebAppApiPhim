using Microsoft.AspNetCore.Mvc;

namespace WebAppApiPhim.Controllers
{
    public class DirectEpisodeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
