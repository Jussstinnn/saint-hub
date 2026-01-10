using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.ViewModels;

namespace SaintHub.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? id, int? fulfillment)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive);

            // ?? FILTRO POR CATEGORÍA (solo si viene)
            if (id.HasValue)
            {
                query = query.Where(p => p.Category == id.Value);
            }

            // ?? FILTRO EN STOCK / POR ENCARGO
            if (fulfillment.HasValue)
            {
                query = query.Where(p => p.Fulfillment == fulfillment.Value);
            }

            var products = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .Select(p => new CategoryProductVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    PriceCrc = p.PriceCrc,
                    Fulfillment = p.Fulfillment,
                    Images = p.Images
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.Url)
                        .ToList()
                })
                .ToListAsync();

            ViewBag.CategoryId = id;
            ViewBag.Fulfillment = fulfillment;

            return View(products);
        }
    }
}
