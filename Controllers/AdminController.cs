using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Models;

namespace SaintHub.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

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
        public IActionResult Products()
        {
            var products = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(products);
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateProduct(Product product, List<IFormFile> images)
        {
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            product.IsActive = true;

            _db.Products.Add(product);
            _db.SaveChanges();

            if (images != null && images.Any())
            {
                bool isFirst = true;

                foreach (var image in images)
                {
                    if (image.Length == 0) continue;

                    var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                    var path = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/uploads/products",
                        fileName
                    );

                    using var stream = new FileStream(path, FileMode.Create);
                    image.CopyTo(stream);

                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Url = "/uploads/products/" + fileName,
                        IsPrimary = isFirst
                    });

                    isFirst = false;
                }

                _db.SaveChanges();
            }

            return RedirectToAction("Products");
        }



        [HttpPost]
        public IActionResult UploadImage(int productId, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return RedirectToAction("Products");

            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            var fileName = $"{Guid.NewGuid()}_{image.FileName}";
            var path = Path.Combine(Directory.GetCurrentDirectory(),
                                    "wwwroot/uploads/products",
                                    fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                image.CopyTo(stream);
            }

            _db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                Url = "/uploads/products/" + fileName,
                IsPrimary = !_db.ProductImages.Any(i => i.ProductId == productId)
            });

            _db.SaveChanges();

            return RedirectToAction("Products");
        }

        [HttpPost]
        public IActionResult AddVariant(int productId, string option, int stock)
        {
            if (string.IsNullOrWhiteSpace(option))
            {
                TempData["Error"] = "La talla no puede estar vacía.";
                return RedirectToAction("Products");
            }

            if (stock < 0)
            {
                TempData["Error"] = "El stock no puede ser negativo.";
                return RedirectToAction("Products");
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

            return RedirectToAction("Products");
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


