using Microsoft.AspNetCore.Mvc;

namespace WebAppApiPhim.Controllers.Api
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
