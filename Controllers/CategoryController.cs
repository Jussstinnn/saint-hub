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

        public IActionResult Index(int id, int? fulfillment)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .Where(p => p.Category == id && p.IsActive);

            // ?? FILTRO EN STOCK / POR ENCARGO
            if (fulfillment.HasValue)
            {
                query = query.Where(p => p.Fulfillment == fulfillment.Value);
            }

            var products = query
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
                .ToList();

            ViewBag.CategoryId = id;
            ViewBag.Fulfillment = fulfillment;

            return View(products);
        }
    }
}
