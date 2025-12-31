using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Models;
using SaintHub.Services;
using SaintHub.ViewModels;
using System.Text.Json;

namespace SaintHub.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _db;

        private const string CART_KEY = "SAINT_CART";
        private const string CHECKOUT_DATA_KEY = "CHECKOUT_DATA";

        public CheckoutController(AppDbContext db)
        {
            _db = db;
        }

        // ============================
        // CHECKOUT
        // GET /Checkout
        // ============================
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY)
                       ?? new List<CartItemVM>();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            ViewBag.Total = cart.Sum(x => x.PriceCrc * x.Quantity);
            return View(cart);
        }

        // ============================
        // PRE-CONFIRM (INTERMEDIO)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PreConfirm(
     string FullName,
     string Phone,
     string Email,
     string Address,
     string PaymentMethod,
     string Notes
 )
        {
            var data = new CheckoutTempData
            {
                FullName = FullName,
                Phone = Phone,
                Email = Email,
                Address = Address,
                PaymentMethod = PaymentMethod,
                Notes = Notes
            };

            TempData[CHECKOUT_DATA_KEY] =
                JsonSerializer.Serialize(data);

            var userId = HttpContext.Session.GetString("USER_AUTH");

            // Invitado ? advertencia
            if (string.IsNullOrEmpty(userId))
                return View("GuestWarning");

            // Logeado ? confirmar DIRECTO (sin redirect)
            return ConfirmPost();
        }

        // ============================
        // CONFIRM POST (REAL)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPost()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemVM>>(CART_KEY);

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            if (!TempData.ContainsKey(CHECKOUT_DATA_KEY))
                return RedirectToAction("Index");

            var data = JsonSerializer.Deserialize<CheckoutTempData>(
                TempData[CHECKOUT_DATA_KEY]!.ToString()!
            )!;

            // USER LOGUEADO (si existe)
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            int? userId = null;

            if (int.TryParse(userIdStr, out var parsedId))
                userId = parsedId;

            using var transaction = _db.Database.BeginTransaction();

            try
            {
                // ============================
                // VALIDAR STOCK
                // ============================
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

                // ============================
                // CREAR PEDIDO
                // ============================
                var order = new Order
                {
                    UserId = userId,

                    FullName = data.FullName,
                    Phone = data.Phone,
                    Email = data.Email,
                    Address = data.Address,
                    PaymentMethod = data.PaymentMethod,
                    Notes = data.Notes,

                    TotalCrc = (int)cart.Sum(i => i.PriceCrc * i.Quantity),
                    Status = "Pendiente",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                _db.SaveChanges();

                // ============================
                // ITEMS + DESCONTAR STOCK
                // ============================
                foreach (var item in cart)
                {
                    var variant = _db.ProductVariants
                        .First(v => v.Id == item.VariantId);

                    variant.Stock -= item.Quantity;

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductName = item.ProductName,
                        Option = item.Size,
                        Quantity = item.Quantity,
                        PriceCrc = item.PriceCrc,
                        SubtotalCrc = item.PriceCrc * item.Quantity
                    });
                }

                _db.SaveChanges();
                transaction.Commit();

                // ============================
                // WHATSAPP
                // ============================
                var whatsappText = "Hola, realicé un pedido en Saint Hub.\n";
                whatsappText += $"Número de pedido: #{order.Id}\n\n";

                foreach (var item in cart)
                {
                    whatsappText +=
                        $"- {item.ProductName} ({item.Size}) x{item.Quantity} ?{item.PriceCrc:N0}\n";
                }

                whatsappText += "\n";
                whatsappText += $"Total: ?{order.TotalCrc:N0}\n";
                whatsappText += "Quedo atento para coordinar pago y entrega.";

                TempData["WhatsappText"] =
                    Uri.EscapeDataString(whatsappText);


                // ============================
                // LIMPIAR CARRITO
                // ============================
                HttpContext.Session.Remove(CART_KEY);

                // ============================
                // SUCCESS
                // ============================
                TempData["OrderId"] = order.Id;
                return RedirectToAction("Success");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ============================
        // CONFIRM (GET — BLOQUEO 405)
        // ============================
        [HttpGet]
        public IActionResult Confirm()
        {
            // Nunca debe ejecutarse por GET
            return RedirectToAction("Index");
        }

        // ============================
        // SUCCESS
        // ============================
        public IActionResult Success()
        {
            if (!TempData.ContainsKey("OrderId"))
                return RedirectToAction("Index", "Cart");

            ViewBag.OrderId = TempData["OrderId"];
            return View();
        }
    }

    // ============================
    // DTO TEMPORAL CHECKOUT
    // ============================
    internal class CheckoutTempData
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
