using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.ViewModels;
using SaintHub.Services;

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

            string NormalizeUrl(string? u)
            {
                if (string.IsNullOrWhiteSpace(u)) return "/img/no-image.png";
                if (u.StartsWith("~/")) u = u.Replace("~/", "/");
                if (!u.StartsWith("/")) u = "/" + u;
                return u;
            }

            var raw = _context.Products
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

            var results = raw.Select(x => new { x.Id, x.Name, Image = NormalizeUrl(x.Image), x.PriceCrc }).ToList();
            return Json(results);
        }

        public IActionResult Index(int? id, int? fulfillment, string? q)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .AsNoTracking()
                .Where(p => p.IsActive);

            // Filtro por categoría (si viene)
            if (id.HasValue)
                query = query.Where(p => p.Category == id.Value);

            // En stock / por encargo
            if (fulfillment.HasValue)
                query = query.Where(p => p.Fulfillment == fulfillment.Value);

            // Search
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.Name.Contains(q) || (p.Description != null && p.Description.Contains(q)));
            }

            var list = query
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var products = list.Select(p =>
            {
                var (fromCrc, toCrc, isRange, note) = ProductRules.GetDisplayPrice(p);

                // Normaliza URLs y garantiza un fallback (evita caídas cuando un producto no tiene imágenes)
                var images = (p.Images ?? new List<SaintHub.Models.ProductImage>())
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u.StartsWith("~/") ? u.Replace("~/", "/") : (u.StartsWith("/") ? u : "/" + u))
                    .ToList();

                if (images.Count == 0)
                    images.Add("/img/no-image.png");

                return new CategoryProductVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    Fulfillment = p.Fulfillment,
                    PriceCrc = p.PriceCrc,
                    PriceFromCrc = fromCrc,
                    PriceToCrc = toCrc,
                    IsPriceRange = isRange,
                    PriceNote = note,
                    Images = images
                };
            }).ToList();

            ViewBag.SearchQuery = q;
            return View(products);
        }

    }
}
