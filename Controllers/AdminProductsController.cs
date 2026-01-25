using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Models;
using SaintHub.Services;
using SaintHub.ViewModels;

namespace SaintHub.Controllers
{
    [Route("Admin/Products")]
    public class AdminProductsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminProductsController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private string WebRootPathSafe => _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        private string UploadDir => Path.Combine(WebRootPathSafe, "uploads", "products");

        // =========================
        // LISTADO
        // GET /Admin/Products
        // =========================
        [HttpGet("")]
        public IActionResult Index()
        {
            var products = _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(products);
        }

        // =========================
        // EDIT (GET)
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

            string NormalizeUrl(string? u)
            {
                if (string.IsNullOrWhiteSpace(u)) return u ?? string.Empty;
                if (u.StartsWith("~/")) u = u.Replace("~/", "/");
                if (!u.StartsWith("/") && !u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    u = "/" + u;
                return u;
            }

            foreach (var img in product.Images)
                img.Url = NormalizeUrl(img.Url);

            var vm = new AdminProductVariantsByColorVM
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                PriceCrc = product.PriceCrc,
                Category = product.Category,
                Fulfillment = product.Fulfillment,
                IsActive = product.IsActive,
                OnDemandFixedPriceCrc = product.OnDemandFixedPriceCrc,
                OnDemandMinPriceCrc = product.OnDemandMinPriceCrc,
                OnDemandMaxPriceCrc = product.OnDemandMaxPriceCrc,

                Images = product.Images.OrderBy(i => i.SortOrder).ToList(),
                Variants = product.Variants.OrderByDescending(v => v.IsActive).ThenBy(v => v.Color).ThenBy(v => v.Option).ToList(),

                Colors = product.Variants
                    .GroupBy(v => v.Color)
                    .Select(g => new ColorGroupVM
                    {
                        Color = g.Key,
                        Variants = g
                            .OrderByDescending(v => v.IsActive)
                            .ThenBy(v => v.Option)
                            .Select(v => new VariantVM
                            {
                                VariantId = v.Id,
                                Option = v.Option,
                                Stock = v.Stock,
                                PriceCrc = v.PriceCrc,
                                IsActive = v.IsActive
                            }).ToList()
                    }).OrderBy(x => x.Color).ToList()
            };

            return View(vm);
        }

        // =========================
        // UPDATE INFO (AJAX)
        // POST /Admin/Products/UpdateInfo
        // =========================
        [HttpPost("UpdateInfo")]
        public async Task<IActionResult> UpdateInfo([FromBody] UpdateProductInfoVM model)
        {
            if (model == null) return BadRequest();

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (product == null) return NotFound();

            product.Name = (model.Name ?? "").Trim();
            product.Description = model.Description;
            product.PriceCrc = model.PriceCrc;
            product.Category = model.Category;
            product.Fulfillment = model.Fulfillment;
            product.IsActive = model.IsActive;

            product.OnDemandFixedPriceCrc = model.OnDemandFixedPriceCrc;
            product.OnDemandMinPriceCrc = model.OnDemandMinPriceCrc;
            product.OnDemandMaxPriceCrc = model.OnDemandMaxPriceCrc;

            ProductRules.NormalizeOnDemandPricing(product, model.OnDemandMode ?? "");
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Stock products: keep ALL variants aligned to the single product price
            if (product.Fulfillment == ProductRules.EnStock)
            {
                var variants = await _db.ProductVariants.Where(v => v.ProductId == product.Id).ToListAsync();
                foreach (var v in variants)
                    v.PriceCrc = product.PriceCrc;

                await _db.SaveChangesAsync();
            }

            // Make sure we always have at least one variant (so product can be added to cart)
            if (!await _db.ProductVariants.AnyAsync(v => v.ProductId == product.Id))
            {
                _db.ProductVariants.Add(new ProductVariant
                {
                    ProductId = product.Id,
                    Color = "Default",
                    Option = "Único",
                    PriceCrc = product.PriceCrc,
                    Stock = 0,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // =========================
        // UPDATE INFO (FORM fallback)
        // POST /Admin/Products/Update
        // =========================
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public IActionResult Update(AdminProductVariantsByColorVM model, string? onDemandMode)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == model.Id);
            if (product == null) return NotFound();

            product.Name = model.Name;
            product.Description = model.Description;
            product.PriceCrc = model.PriceCrc;
            product.Category = model.Category;
            product.Fulfillment = model.Fulfillment;
            product.IsActive = model.IsActive;

            product.OnDemandFixedPriceCrc = model.OnDemandFixedPriceCrc;
            product.OnDemandMinPriceCrc = model.OnDemandMinPriceCrc;
            product.OnDemandMaxPriceCrc = model.OnDemandMaxPriceCrc;

            ProductRules.NormalizeOnDemandPricing(product, onDemandMode ?? "");
            product.UpdatedAt = DateTime.UtcNow;

            _db.SaveChanges();

            // Stock products: keep ALL variants aligned to the single product price
            if (product.Fulfillment == 1)
            {
                var variants = _db.ProductVariants.Where(v => v.ProductId == product.Id).ToList();
                foreach (var v in variants) v.PriceCrc = product.PriceCrc;
                _db.SaveChanges();
            }

            return RedirectToAction("Edit", new { id = product.Id });
        }

        // =========================
        // UPLOAD IMAGES (AJAX)
        // POST /Admin/Products/UploadImages
        // =========================
        [HttpPost("UploadImages")]
        public async Task<IActionResult> UploadImages(int productId, List<IFormFile> images)
        {
            var product = await _db.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();
            if (images == null || images.Count == 0) return BadRequest();

            Directory.CreateDirectory(UploadDir);

            var sort = product.Images.Select(i => (int?)i.SortOrder).Max() ?? 0;
            var hasPrimary = product.Images.Any(i => i.IsPrimary);

            var added = new List<object>();
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
                    await using var stream = new FileStream(path, FileMode.Create);
                    await image.CopyToAsync(stream);
                }
                catch
                {
                    rejected.Add(Path.GetFileName(image.FileName));
                    continue;
                }

                sort += 1;
                var setPrimary = !hasPrimary;
                if (setPrimary) hasPrimary = true;

                var img = new ProductImage
                {
                    ProductId = product.Id,
                    Url = "/uploads/products/" + fileName,
                    IsPrimary = setPrimary,
                    SortOrder = sort
                };

                _db.ProductImages.Add(img);
                await _db.SaveChangesAsync();

                added.Add(new { id = img.Id, url = img.Url, isPrimary = img.IsPrimary, sortOrder = img.SortOrder });
            }

            return Json(new { success = true, images = added, rejected });
        }

        // =========================
        // REORDER IMAGES (AJAX)
        // POST /Admin/Products/ReorderImages
        // =========================
        public class ReorderImagesRequest
        {
            public int ProductId { get; set; }
            public List<int> ImageIds { get; set; } = new();
        }

        [HttpPost("ReorderImages")]
        public async Task<IActionResult> ReorderImages([FromBody] ReorderImagesRequest req)
        {
            if (req == null || req.ProductId <= 0 || req.ImageIds == null) return BadRequest();

            var images = await _db.ProductImages
                .Where(i => i.ProductId == req.ProductId)
                .ToListAsync();

            if (images.Count == 0) return Json(new { success = true });

            // Only keep ids that exist
            var ordered = req.ImageIds.Where(id => images.Any(i => i.Id == id)).ToList();

            // Append any missing ids (safety)
            foreach (var missing in images.Select(i => i.Id).Where(id => !ordered.Contains(id)))
                ordered.Add(missing);

            for (var idx = 0; idx < ordered.Count; idx++)
            {
                var img = images.First(i => i.Id == ordered[idx]);
                img.SortOrder = idx + 1;
                img.IsPrimary = idx == 0;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // =========================
        // SET PRIMARY (fallback)
        // POST /Admin/Products/SetPrimaryImage
        // =========================
        [HttpPost("SetPrimaryImage")]
        [ValidateAntiForgeryToken]
        public IActionResult SetPrimaryImage(int imageId, int productId)
        {
            var images = _db.ProductImages.Where(i => i.ProductId == productId).ToList();
            if (images.Count == 0) return RedirectToAction("Edit", new { id = productId });

            foreach (var img in images)
                img.IsPrimary = img.Id == imageId;

            _db.SaveChanges();
            return RedirectToAction("Edit", new { id = productId });
        }

        // =========================
        // DELETE IMAGE (both form + ajax)
        // POST /Admin/Products/DeleteImage
        // =========================
        [HttpPost("DeleteImage")]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            var img = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);
            if (img == null) return Json(new { success = false });

            // Delete file from disk (best-effort)
            try
            {
                if (!string.IsNullOrWhiteSpace(img.Url) && img.Url.StartsWith("/"))
                {
                    var physical = Path.Combine(WebRootPathSafe, img.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
                }
            }
            catch { /* ignore */ }

            _db.ProductImages.Remove(img);
            await _db.SaveChangesAsync();

            // Re-normalize order + primary
            var remaining = await _db.ProductImages.Where(i => i.ProductId == productId).OrderBy(i => i.SortOrder).ToListAsync();
            for (var i = 0; i < remaining.Count; i++)
            {
                remaining[i].SortOrder = i + 1;
                remaining[i].IsPrimary = i == 0;
            }
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // =========================
        // UPDATE IMAGE COLOR (AJAX)
        // POST /Admin/Products/UpdateImageColor
        // =========================
        [HttpPost("UpdateImageColor")]
        public async Task<IActionResult> UpdateImageColor([FromBody] SaintHub.ViewModels.UpdateImageColorVM vm)
        {
            if (vm == null || vm.ProductId <= 0 || vm.ImageId <= 0) return BadRequest();

            var img = await _db.ProductImages
                .FirstOrDefaultAsync(i => i.Id == vm.ImageId && i.ProductId == vm.ProductId);

            if (img == null) return NotFound();

            img.Color = string.IsNullOrWhiteSpace(vm.Color) ? null : vm.Color.Trim();
            await _db.SaveChangesAsync();

            return Ok();
        }

        // =========================
        // ADD VARIANT (existing)
        // POST /Admin/Products/AddVariant
        // =========================
        [HttpPost("AddVariant")]
        public IActionResult AddVariant(int productId, string option, string color, int stock, int priceCrc)
        {
            if (string.IsNullOrWhiteSpace(option))
            {
                TempData["Error"] = "La opción no puede estar vacía.";
                return RedirectToAction("Edit", new { id = productId });
            }

            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            // Stock products: price is fixed per product
            if (product.Fulfillment == ProductRules.EnStock)
                priceCrc = product.PriceCrc;

            _db.ProductVariants.Add(new ProductVariant
            {
                ProductId = productId,
                Option = option.Trim(),
                Color = (color ?? "Default").Trim(),
                Stock = stock,
                PriceCrc = priceCrc,
                IsActive = true
            });

            _db.SaveChanges();
            return RedirectToAction("Edit", new { id = productId });
        }

        // =========================
        // INLINE VARIANT UPDATES (AJAX)
        // =========================
        [HttpPost("UpdateVariantOption")]
        public async Task<IActionResult> UpdateVariantOption([FromBody] UpdateVariantOptionVM vm)
        {
            var variant = await _db.ProductVariants.FindAsync(vm.VariantId);
            if (variant == null) return NotFound();

            variant.Option = (vm.Option ?? "").Trim();
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("UpdateVariantStock")]
        public async Task<IActionResult> UpdateVariantStock([FromBody] UpdateVariantStockVM vm)
        {
            var variant = await _db.ProductVariants.FindAsync(vm.VariantId);
            if (variant == null) return NotFound();

            variant.Stock = vm.Stock;
            await _db.SaveChangesAsync();
            return Ok();
        }

        
[HttpPost("UpdateVariantPrice")]
public async Task<IActionResult> UpdateVariantPrice([FromBody] UpdateVariantPriceVM vm)
{
    var variant = await _db.ProductVariants
        .Include(v => v.Product)
        .Where(v => v.Id == vm.VariantId)
        .FirstOrDefaultAsync();

    if (variant == null) return NotFound();

    // Stock products: single fixed price driven by Product.PriceCrc.
    if (variant.Product != null && variant.Product.Fulfillment == ProductRules.EnStock)
    {
        variant.Product.PriceCrc = vm.PriceCrc;

        var siblings = await _db.ProductVariants
            .Where(v => v.ProductId == variant.ProductId)
            .ToListAsync();

        foreach (var s in siblings)
            s.PriceCrc = vm.PriceCrc;

        await _db.SaveChangesAsync();
        return Ok();
    }

    variant.PriceCrc = vm.PriceCrc;
    await _db.SaveChangesAsync();
    return Ok();
}

        [HttpPost("ToggleVariantStatus")]
        public async Task<IActionResult> ToggleVariantStatus([FromBody] ToggleVariantStatusVM vm)
        {
            var variant = await _db.ProductVariants.FindAsync(vm.VariantId);
            if (variant == null) return NotFound();

            variant.IsActive = vm.IsActive;
            await _db.SaveChangesAsync();
            return Ok();
        }

        // =========================
        // DELETE PRODUCT
        // POST /Admin/Products/Delete/5
        // =========================
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return RedirectToAction("Index");

            // Delete image files (best-effort)
            foreach (var img in product.Images)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(img.Url) && img.Url.StartsWith("/"))
                    {
                        var physical = Path.Combine(WebRootPathSafe, img.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
                    }
                }
                catch { /* ignore */ }
            }

            _db.ProductImages.RemoveRange(product.Images);
            _db.ProductVariants.RemoveRange(product.Variants);
            _db.Products.Remove(product);

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
