using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
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

            if (variant == null || variant.Stock <= 0)
                return Json(new { success = false });

            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY)
                       ?? new List<CartItemVM>();

            var item = cart.FirstOrDefault(c => c.VariantId == variantId);

            if (item != null)
            {
                if (item.Quantity < item.Stock)
                    item.Quantity++;
            }
            else
            {
                // =========================
                // ?? IMAGEN SEGÚN COLOR
                // =========================
                var imageForColor = variant.Product.Images
                    .FirstOrDefault(i =>
                        !string.IsNullOrEmpty(i.Color) &&
                        i.Color.ToLower() == variant.Color.ToLower()
                    )
                    ?? variant.Product.Images.FirstOrDefault(i => i.IsPrimary)
                    ?? variant.Product.Images.FirstOrDefault();

                cart.Add(new CartItemVM
                {
                    ProductId = variant.ProductId,
                    VariantId = variant.Id,
                    ProductName = variant.Product.Name,

                    // ? AQUÍ QUEDA LA IMAGEN CORRECTA
                    ImageUrl = imageForColor?.Url ?? "",

                    Size = variant.Option,
                    Quantity = 1,
                    Stock = variant.Stock,
                    PriceCrc = variant.PriceCrc
                });
            }

            HttpContext.Session.SetObject(CART_KEY, cart);

            var count = cart.Sum(x => x.Quantity);

            return Json(new { success = true, count });
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

            if (item != null && item.Quantity < item.Stock)
            {
                item.Quantity++;
                HttpContext.Session.SetObject(CART_KEY, cart);
            }

            var count = cart.Sum(x => x.Quantity);

            return Json(new { count });
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

            var count = cart.Sum(x => x.Quantity);

            return Json(new { count });
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
            return RedirectToAction("Index");
        }
    }
}
