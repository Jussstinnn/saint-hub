using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Models;

namespace SaintHub.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _db;

        public ShopController(AppDbContext db)
        {
            _db = db;
        }

        // /Shop
        // /Shop?category=1&q=air
        public async Task<IActionResult> Index(int? category, int? fulfillment, string? q)

        {
            var query = _db.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive);

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }
            if (fulfillment.HasValue)
            {
                query = query.Where(p => p.Fulfillment == fulfillment.Value);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Name.Contains(q));
            }

            var products = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .Take(60)
                .ToListAsync();

            return View(products);
        }

        // /Shop/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = _db.Products
    .Include(p => p.Images)
    .Include(p => p.Variants.Where(v => v.IsActive))
    .FirstOrDefault(p => p.Id == id);


            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}

