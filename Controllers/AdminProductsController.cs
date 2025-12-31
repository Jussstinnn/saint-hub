using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Models;
using SaintHub.ViewModels;
using SaintHub.ViewModel;

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

        // =========================
        // LISTADO DE PRODUCTOS
        // GET /Admin/Products
        // =========================
        [HttpGet("")]
        public IActionResult Index()
        {
            var products = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(products);
        }

        // =========================
        // EDITAR PRODUCTO (GET)
        // GET /Admin/Products/Edit/5
        // =========================
        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var product = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            var vm = new AdminProductVariantsByColorVM
            {
                // ===== INFO GENERAL
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                PriceCrc = product.PriceCrc,
                IsActive = product.IsActive,

                // ===== GALERÍA
                Images = product.Images,

                // ===== VARIANTES PLANAS (para mantener la vista actual)
                Variants = product.Variants,

                // ===== VARIANTES AGRUPADAS POR COLOR (NUEVO)
                Colors = product.Variants
                    .GroupBy(v => v.Color)
                    .Select(g => new ColorGroupVM
                    {
                        Color = g.Key,
                        Variants = g.Select(v => new VariantVM
                        {
                            VariantId = v.Id,
                            Option = v.Option,
                            Stock = v.Stock,
                            PriceCrc = v.PriceCrc,
                            IsActive = v.IsActive
                        }).ToList()
                    })
                    .OrderBy(c => c.Color)
                    .ToList()
            };

            return View(vm);
        }

        // =========================
        // ACTUALIZAR PRODUCTO (POST)
        // POST /Admin/Products/Edit/5
        // =========================
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public IActionResult Update(int id, AdminProductVariantsByColorVM model)

        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return RedirectToAction("Edit", new { id });

            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound();

            product.Name = model.Name;
            product.Description = model.Description;
            product.PriceCrc = model.PriceCrc;
            product.Fulfillment = model.Fulfillment;
            product.IsActive = model.IsActive;
            product.UpdatedAt = DateTime.Now;

            _db.SaveChanges();

            return RedirectToAction("Edit", new { id });
        }

        // =========================
        // AGREGAR VARIANTE
        // POST /Admin/Products/AddVariant
        // =========================
        [HttpPost("AddVariant")]
        [ValidateAntiForgeryToken]
        public IActionResult AddVariant(
            int productId,
            string option,
            string color,
            int priceCrc,
            int stock)
        {
            if (string.IsNullOrWhiteSpace(option) || string.IsNullOrWhiteSpace(color))
            {
                TempData["Error"] = "Talla y color son obligatorios.";
                return RedirectToAction("Edit", new { id = productId });
            }

            if (priceCrc < 0 || stock < 0)
            {
                TempData["Error"] = "Precio y stock no pueden ser negativos.";
                return RedirectToAction("Edit", new { id = productId });
            }

            _db.ProductVariants.Add(new ProductVariant
            {
                ProductId = productId,
                Option = option.Trim(),
                Color = color.Trim(),
                PriceCrc = priceCrc,
                Stock = stock,
                IsActive = true
            });

            _db.SaveChanges();

            return RedirectToAction("Edit", new { id = productId });
        }

        // =========================
        // ACTUALIZAR VARIANTE (FORM ACTUAL)
        // POST /Admin/Products/UpdateVariant
        // =========================
        [HttpPost("UpdateVariant")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateVariant(
            int variantId,
            int productId,
            string option,
            string color,
            int priceCrc,
            int stock)
        {
            var variant = _db.ProductVariants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
                return RedirectToAction("Edit", new { id = productId });

            variant.Option = option.Trim();
            variant.Color = color.Trim();
            variant.PriceCrc = priceCrc;
            variant.Stock = stock;

            _db.SaveChanges();

            return RedirectToAction("Edit", new { id = productId });
        }

        // =========================
        // SUBIR IMAGEN
        // POST /Admin/Products/AddImage
        // =========================
        [HttpPost("AddImage")]
        [ValidateAntiForgeryToken]
        public IActionResult AddImage(int productId, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return RedirectToAction("Edit", new { id = productId });

            var product = _db.Products
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return NotFound();

            var uploadsPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/uploads/products"
            );

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(stream);
            }

            _db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                Url = "/uploads/products/" + fileName,
                IsPrimary = !product.Images.Any()
            });

            _db.SaveChanges();

            return RedirectToAction("Edit", new { id = productId });
        }

        // =========================
        // ACTUALIZAR COLOR DE IMAGEN
        // POST /Admin/Products/UpdateImageColor
        // =========================
        [HttpPost("UpdateImageColor")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateImageColor(int imageId, int productId, string? color)
        {
            var image = _db.ProductImages.FirstOrDefault(i => i.Id == imageId);
            if (image == null)
                return RedirectToAction("Edit", new { id = productId });

            image.Color = string.IsNullOrWhiteSpace(color)
                ? null
                : color.Trim();

            _db.SaveChanges();

            return RedirectToAction("Edit", new { id = productId });
        }
        // =========================
        // ACTIVAR / DESACTIVAR VARIANTE (AJAX)
        // POST /Admin/Products/ToggleVariantStatus
        // =========================
        [HttpPost("ToggleVariantStatus")]
        public IActionResult ToggleVariantStatus([FromBody] ToggleVariantStatusVM data)
        {
            var variant = _db.ProductVariants.FirstOrDefault(v => v.Id == data.VariantId);
            if (variant == null)
                return NotFound();

            variant.IsActive = data.IsActive;
            _db.SaveChanges();

            return Ok();
        }
        // =========================
        // ACTUALIZAR TALLA INLINE (AJAX)
        // POST /Admin/Products/UpdateVariantOption
        // =========================
        [HttpPost("UpdateVariantOption")]
        public IActionResult UpdateVariantOption(
    [FromBody] SaintHub.ViewModel.UpdateVariantOptionVM data)
        {
            if (string.IsNullOrWhiteSpace(data.Option))
                return BadRequest();

            var variant = _db.ProductVariants
                .FirstOrDefault(v => v.Id == data.VariantId);

            if (variant == null)
                return NotFound();

            variant.Option = data.Option.Trim();
            _db.SaveChanges();

            return Ok();
        }




        // =========================
        // ACTUALIZAR PRECIO INLINE (AJAX)
        // POST /Admin/Products/UpdateVariantPrice
        // =========================
        [HttpPost("UpdateVariantPrice")]
        public IActionResult UpdateVariantPrice([FromBody] UpdateVariantPriceVM data)
        {
            var variant = _db.ProductVariants.FirstOrDefault(v => v.Id == data.VariantId);
            if (variant == null)
                return NotFound();

            if (data.PriceCrc < 0)
                return BadRequest();

            variant.PriceCrc = data.PriceCrc;
            _db.SaveChanges();

            return Ok();
        }

        // =========================
        // ACTUALIZAR STOCK INLINE (AJAX)
        // POST /Admin/Products/UpdateVariantStock
        // =========================
        [HttpPost("UpdateVariantStock")]
        public IActionResult UpdateVariantStock([FromBody] UpdateVariantStockVM data)
        {
            var variant = _db.ProductVariants.FirstOrDefault(v => v.Id == data.VariantId);
            if (variant == null)
                return NotFound();

            if (data.Stock < 0)
                return BadRequest();

            variant.Stock = data.Stock;
            _db.SaveChanges();

            return Ok();
        }

        // =========================
        // ELIMINAR IMAGEN
        // POST /Admin/Products/DeleteImage
        // =========================
        [HttpPost("DeleteImage")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteImage(int imageId, int productId)
        {
            var image = _db.ProductImages.FirstOrDefault(i => i.Id == imageId);
            if (image == null)
                return RedirectToAction("Edit", new { id = productId });

            var productImages = _db.ProductImages
                .Where(i => i.ProductId == productId)
                .ToList();

            if (productImages.Count == 1)
            {
                TempData["Error"] = "El producto debe tener al menos una imagen.";
                return RedirectToAction("Edit", new { id = productId });
            }

            if (image.IsPrimary)
            {
                var newPrimary = productImages.FirstOrDefault(i => i.Id != image.Id);
                if (newPrimary != null)
                    newPrimary.IsPrimary = true;
            }

            var physicalPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                image.Url.TrimStart('/')
            );

            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);

            _db.ProductImages.Remove(image);
            _db.SaveChanges();

            return RedirectToAction("Edit", new { id = productId });
        }
    }
}
