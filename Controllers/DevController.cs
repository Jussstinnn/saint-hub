using Microsoft.AspNetCore.Mvc;

namespace SaintHub.Controllers
{
    public class DevController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}