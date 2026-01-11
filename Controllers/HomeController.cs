using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using System;

namespace SaintHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var feature = _db.HomeFeatureSections.FirstOrDefault(x => x.IsActive);
            return View(feature);
        }


        public IActionResult TestDb()
        {
            var count = _db.Products.Count();
            return Content($"Productos en BD: {count}");
        }
    }
}

