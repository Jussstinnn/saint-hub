using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Models;

namespace SaintHub.Controllers
{
    public class CustomOrderController : Controller
    {
        private readonly AppDbContext _context;

        public CustomOrderController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new CustomRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(CustomRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Content("MODELSTATE INVALIDO");
            }

            try
            {
                request.Status = "Nuevo";
                request.CreatedAt = DateTime.Now;

                _context.CustomRequests.Add(request);
                _context.SaveChanges();

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                return Content("ERROR DB: " + ex.Message);
            }
        }




        public IActionResult Success()
        {
            return View();
        }
    }
}



