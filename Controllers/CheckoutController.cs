using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Models;
using SaintHub.Services;
using SaintHub.ViewModels;

namespace SaintHub.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _db;
        private const string CART_KEY = "SAINT_CART";

        public CheckoutController(AppDbContext db)
        {
            _db = db;
        }

        // ============================
        // CHECKOUT
        // ============================
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY)
                       ?? new List<CartItemVM>();

            ViewBag.Total = cart.Sum(x => x.PriceCrc * x.Quantity);

            return View(cart);
        }

        // ============================
        // CONFIRMAR PEDIDO
        // ============================
        [HttpPost]
        public IActionResult Confirm()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY);

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            using var transaction = _db.Database.BeginTransaction();

            try
            {
                // 1?? VALIDAR STOCK
                foreach (var item in cart)
                {
                    var variant = _db.ProductVariants
                        .FirstOrDefault(v => v.Id == item.VariantId);

                    if (variant == null || variant.Stock < item.Quantity)
                    {
                        TempData["Error"] =
                            $"Stock insuficiente para {item.ProductName} ({item.Size}).";

                        return RedirectToAction("Index", "Cart");
                    }
                }

                // 2?? CREAR PEDIDO
                var order = new Order
                {
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pendiente",
                    TotalCrc = (int)cart.Sum(i => i.PriceCrc * i.Quantity)
                };

                _db.Orders.Add(order);
                _db.SaveChanges();

                // 3?? ITEMS + DESCONTAR STOCK
                foreach (var item in cart)
                {
                    var variant = _db.ProductVariants
                        .First(v => v.Id == item.VariantId);

                    variant.Stock -= item.Quantity;

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductName = item.ProductName,
                        Option = item.Size, // talla (viene de Variant.Option)
                        Quantity = item.Quantity,
                        PriceCrc = (int)item.PriceCrc,
                        SubtotalCrc = (int)(item.PriceCrc * item.Quantity)
                    });
                }

                _db.SaveChanges();
                transaction.Commit();

                // 4?? LIMPIAR CARRITO
                HttpContext.Session.Remove(CART_KEY);

                // 5?? CARGAR ITEMS PARA LA VISTA
                var items = _db.OrderItems
                    .Where(i => i.OrderId == order.Id)
                    .ToList();

                ViewBag.Items = items;

                // 6?? WHATSAPP (mensaje bien codificado)
                var mensajePlano =
$@"Hola ??
Nuevo pedido en Saint Hub ???

Pedido: #{order.Id}
Total: ? {order.TotalCrc:N0}
Estado: {order.Status}

Productos:";

                foreach (var i in items)
                {
                    mensajePlano +=
$@"
- {i.ProductName} ({i.Option}) x{i.Quantity}";
                }

                var mensajeCodificado = Uri.EscapeDataString(mensajePlano);

                ViewBag.WhatsappUrl =
                    $"https://wa.me/50671270397?text={mensajeCodificado}";

                return View("Success", order);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ============================
        // SUCCESS (fallback)
        // ============================
        public IActionResult Success(Order order)
        {
            return View(order);
        }
    }
}





