using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Models;

namespace SaintHub.Controllers
{
    [Route("Admin/Products")]
    public class AdminProductsController : Controller
    {
        private readonly AppDbContext _db;

        public AdminProductsController(AppDbContext db)
        {
            _db = db;
        }

        // LISTADO
        [HttpGet("")]
        public IActionResult Index()
        {
            var products = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .ToList();

            return View(products);
        }

        // EDITAR
        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var product = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefault(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }
    }
}



