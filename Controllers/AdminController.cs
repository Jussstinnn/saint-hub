using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Models;
using SaintHub.Services;

namespace SaintHub.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private string WebRootPathSafe => _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        private string UploadDir => Path.Combine(WebRootPathSafe, "uploads", "products");

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string password)
        {
            var adminPass = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetValue<string>("Admin:Password");

            if (password == adminPass)
            {
                HttpContext.Session.SetString("ADMIN_AUTH", "OK");
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Contraseña incorrecta";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("ADMIN_AUTH");
            return RedirectToAction("Login");
        }


        // /Admin
        public IActionResult Index()
        {
            ViewBag.Products = _db.Products.Count();
            ViewBag.Orders = _db.Orders.Count();
            return View();
        }

        // ======================
        // PEDIDOS
        // ======================
        public IActionResult Orders()
        {
            var orders = _db.Orders
     .Include(o => o.Items)
     .OrderByDescending(o => o.CreatedAt)
     .ToList();


            return View(orders);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            order.Status = status;
            _db.SaveChanges();

            return RedirectToAction("Orders");
        }

        // ======================
        // PRODUCTOS
        // ======================
        public IActionResult ProductsLegacy()
        {
            var products = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View("Products", products);
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateProduct(Product product, List<IFormFile> images, int InitialStock = 0, string onDemandMode = "fixed")
        {
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            product.IsActive = true;

            // Normalize on-demand pricing
            ProductRules.NormalizeOnDemandPricing(product, onDemandMode);

            _db.Products.Add(product);
            _db.SaveChanges();

            // Ensure a default variant exists (so the product can be added to cart)
            if (!_db.ProductVariants.Any(v => v.ProductId == product.Id))
            {
                _db.ProductVariants.Add(new ProductVariant
                {
                    ProductId = product.Id,
                    Color = "Default",
                    Option = "Único",
                    PriceCrc = product.PriceCrc,
                    Stock = Math.Max(0, InitialStock),
                    IsActive = true
                });
                _db.SaveChanges();
            }

            if (images != null && images.Any())
            {
                Directory.CreateDirectory(UploadDir);

                var sort = _db.ProductImages
                    .Where(i => i.ProductId == product.Id)
                    .Select(i => (int?)i.SortOrder)
                    .Max() ?? 0;

                var hasPrimary = _db.ProductImages.Any(i => i.ProductId == product.Id && i.IsPrimary);

                var rejected = new List<string>();

                foreach (var image in images)
                {
                    if (image == null || image.Length == 0) continue;

                    // Seguridad + soporte .webp/.avif
                    if (!ImageUploadRules.IsAllowed(image))
                    {
                        rejected.Add(Path.GetFileName(image.FileName));
                        continue;
                    }

                    // Nombre seguro + extensión normalizada (evita issues de content-type/case en Linux)
                    var ext = Path.GetExtension(image.FileName);
                    if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                    ext = ext.ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var path = Path.Combine(UploadDir, fileName);

                    try
                    {
                        using var stream = new FileStream(path, FileMode.Create);
                        image.CopyTo(stream);
                    }
                    catch
                    {
                        rejected.Add(Path.GetFileName(image.FileName));
                        continue;
                    }

                    sort += 1;
                    var setPrimary = !hasPrimary;
                    if (setPrimary) hasPrimary = true;

                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Url = "/uploads/products/" + fileName,
                        IsPrimary = setPrimary,
                        SortOrder = sort
                    });
                }

                _db.SaveChanges();

                if (rejected.Count > 0)
                {
                    TempData["Warning"] = $"Se omitieron {rejected.Count} archivo(s) por formato no permitido. Permitidos: JPG, PNG, GIF, WEBP, AVIF.";
                }
            }

            return RedirectToAction("Index", "AdminProducts");
        }



        [HttpPost]
        public IActionResult UploadImage(int productId, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return RedirectToAction("Index", "AdminProducts");

            if (!ImageUploadRules.IsAllowed(image))
            {
                TempData["Error"] = "Formato de imagen no permitido. Permitidos: JPG, PNG, GIF, WEBP, AVIF.";
                return RedirectToAction("Index", "AdminProducts");
            }

            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            Directory.CreateDirectory(UploadDir);

            // Nombre seguro + extensión normalizada (evita issues de content-type/case en Linux)
            var ext = Path.GetExtension(image.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            ext = ext.ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(UploadDir, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                image.CopyTo(stream);
            }

            var nextSort = (_db.ProductImages
                .Where(i => i.ProductId == productId)
                .Select(i => (int?)i.SortOrder)
                .Max() ?? 0) + 1;

            _db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                Url = "/uploads/products/" + fileName,
                IsPrimary = !_db.ProductImages.Any(i => i.ProductId == productId),
                SortOrder = nextSort
            });

            _db.SaveChanges();

            return RedirectToAction("Index", "AdminProducts");
        }

        [HttpPost]
        public IActionResult AddVariant(int productId, string option, int stock)
        {
            if (string.IsNullOrWhiteSpace(option))
            {
                TempData["Error"] = "La talla no puede estar vacía.";
                return RedirectToAction("Index", "AdminProducts");
            }

            if (stock < 0)
            {
                TempData["Error"] = "El stock no puede ser negativo.";
                return RedirectToAction("Index", "AdminProducts");
            }

            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            _db.ProductVariants.Add(new ProductVariant
            {
                ProductId = productId,
                Option = option.Trim(),
                Stock = stock,
                IsActive = true
            });

            _db.SaveChanges();

            return RedirectToAction("Index", "AdminProducts");
        }
        public IActionResult Details(int id)
        {
            var order = _db.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        public IActionResult Dashboard()
        {
            var orders = _db.Orders.ToList();
            var items = _db.OrderItems.ToList();

            ViewBag.TotalSales = orders.Sum(o => o.TotalCrc);
            ViewBag.TotalOrders = orders.Count;

            ViewBag.ByStatus = orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.TopProducts = items
                .GroupBy(i => i.ProductName)
                .Select(g => new {
                    Name = g.Key,
                    Qty = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.SubtotalCrc)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToList();

            ViewBag.LastOrders = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToList();

            return View();
        }

    }

}


