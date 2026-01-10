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
        [HttpGet]
        public IActionResult SearchPreview(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            var results = _context.Products
                .Where(p => p.IsActive && p.Name.Contains(q))
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    Image = p.Images
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.Url)
                        .FirstOrDefault(),
                    p.PriceCrc
                })
                .ToList();

            return Json(results);
        }

        public IActionResult Index(int? id, int? fulfillment, string? q)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive);

            // ?? Filtro por categoría (si viene)
            if (id.HasValue)
                query = query.Where(p => p.Category == id.Value);

            // ?? En stock / por encargo
            if (fulfillment.HasValue)
                query = query.Where(p => p.Fulfillment == fulfillment.Value);

            // ?? SEARCH GLOBAL
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                query = query.Where(p =>
                    p.Name.Contains(q) ||
                    (p.Description != null && p.Description.Contains(q))
                );
            }

            var products = query
                .OrderByDescending(p => p.CreatedAt)
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

            ViewBag.SearchQuery = q;

            return View(products);
        }

    }
}
