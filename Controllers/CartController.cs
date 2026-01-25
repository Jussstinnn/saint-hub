using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Models;
using SaintHub.Services;
using SaintHub.ViewModels;

namespace SaintHub.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _db;
        private const string CART_KEY = "SAINT_CART";

        public CartController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY)
                       ?? new List<CartItemVM>();

            return View(cart);
        }

        // ============================
        // AGREGAR AL CARRITO
        // ============================
        [HttpPost]
        public IActionResult Add(int variantId)
        {
            var variant = _db.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .FirstOrDefault(v => v.Id == variantId && v.IsActive);

            if (variant == null)
                return Json(new { success = false, message = "Variante no encontrada" });

            var product = variant.Product;
            var requireStock = product.Fulfillment == ProductRules.EnStock;
            if (requireStock && variant.Stock <= 0)
                return Json(new { success = false, message = "Sin stock" });

            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY)
                       ?? new List<CartItemVM>();

            var item = cart.FirstOrDefault(c => c.VariantId == variantId);

            if (item != null)
            {
                if (!requireStock || item.Quantity < item.Stock)
                    item.Quantity++;
            }
            else
            {
                // Imagen según color
                var imageForColor = product.Images
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i.Color) &&
                                         i.Color!.ToLower() == (variant.Color ?? "").ToLower())
                    ?? product.Images.FirstOrDefault(i => i.IsPrimary)
                    ?? product.Images.FirstOrDefault();

                var (unit, min, max) = ProductRules.GetCartUnitPrice(product);

                cart.Add(new CartItemVM
                {
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    ProductName = product.Name,
                    ImageUrl = imageForColor?.Url ?? "",

                    Category = product.Category,
                    Fulfillment = product.Fulfillment,
                    OptionLabel = ProductRules.OptionLabel(product.Category),
                    OptionValue = ProductRules.IsVinyl(product.Category) ? "" : (variant.Option ?? ""),

                    Quantity = 1,
                    Stock = requireStock ? variant.Stock : 9999,

                    UnitPriceCrc = unit,
                    UnitPriceMinCrc = min,
                    UnitPriceMaxCrc = max
                });
            }

            HttpContext.Session.SetObject(CART_KEY, cart);

            var summary = BuildSummary(cart, variantId);
            return Json(new { success = true, cartCount = summary.cartCount, summary });
        }


        // ============================
        // AUMENTAR CANTIDAD
        // ============================
        [HttpPost]
        public IActionResult Increase(int variantId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY)
                       ?? new List<CartItemVM>();

            var item = cart.FirstOrDefault(x => x.VariantId == variantId);
            if (item != null)
            {
                var requireStock = item.Fulfillment == ProductRules.EnStock;
                if (!requireStock || item.Quantity < item.Stock)
                {
                    item.Quantity++;
                    HttpContext.Session.SetObject(CART_KEY, cart);
                }
            }

            var summary = BuildSummary(cart, variantId);
            return Json(new { success = true, cartCount = summary.cartCount, summary });
        }

        // ============================
        // DISMINUIR CANTIDAD
        // ============================
        [HttpPost]
        public IActionResult Decrease(int variantId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY)
                       ?? new List<CartItemVM>();

            var item = cart.FirstOrDefault(x => x.VariantId == variantId);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                    cart.Remove(item);

                HttpContext.Session.SetObject(CART_KEY, cart);
            }

            var summary = BuildSummary(cart, variantId);
            return Json(new { success = true, cartCount = summary.cartCount, summary });
        }

        // ============================
        // QUITAR ITEM
        // ============================
        [HttpPost]
        public IActionResult Remove(int variantId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY) ?? new();
            cart.RemoveAll(x => x.VariantId == variantId);
            HttpContext.Session.SetObject(CART_KEY, cart);

            var summary = BuildSummary(cart, variantId);
            return Json(new { success = true, cartCount = summary.cartCount, summary });
        }

        private (int cartCount, decimal totalMin, decimal totalMax, bool isRange, object? item) BuildSummary(List<CartItemVM> cart, int variantId)
        {
            var cartCount = cart.Sum(x => x.Quantity);

            decimal totalMin = 0;
            decimal totalMax = 0;
            foreach (var x in cart)
            {
                var uMin = (x.UnitPriceMinCrc ?? (int)x.UnitPriceCrc);
                var uMax = (x.UnitPriceMaxCrc ?? (int)x.UnitPriceCrc);
                totalMin += uMin * x.Quantity;
                totalMax += uMax * x.Quantity;
            }

            var isRange = totalMin != totalMax;
            var it = cart.FirstOrDefault(x => x.VariantId == variantId);
            object? item = null;
            if (it != null)
            {
                var uMin = (it.UnitPriceMinCrc ?? (int)it.UnitPriceCrc);
                var uMax = (it.UnitPriceMaxCrc ?? (int)it.UnitPriceCrc);
                var subMin = uMin * it.Quantity;
                var subMax = uMax * it.Quantity;
                item = new { variantId = it.VariantId, qty = it.Quantity, subMin, subMax, isRange = (subMin != subMax) };
            }

            return (cartCount, totalMin, totalMax, isRange, item);
        }
    }
}

