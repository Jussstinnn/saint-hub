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

        public IActionResult Index(int id)
        {
            var products = _context.Products
                .Include(p => p.Images) // ? IMPORTANTE
                .Where(p => p.Category == id && p.IsActive)
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
                .ToList(); // ? CLAVE

            ViewBag.CategoryId = id;

            return View(products);
        }
    }
}
