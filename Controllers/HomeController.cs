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
            return View();
        }

        public IActionResult TestDb()
        {
            var count = _db.Products.Count();
            return Content($"Productos en BD: {count}");
        }
    }
}

