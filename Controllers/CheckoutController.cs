using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IOrderAlertService _alerts;

        private const string CART_KEY = "SAINT_CART";
        private const string CHECKOUT_DATA_KEY = "CHECKOUT_DATA";

        public CheckoutController(AppDbContext db, IOrderAlertService alerts)
        {
            _db = db;
            _alerts = alerts;
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

            ViewBag.TotalMin = cart.Sum(x => x.SubtotalMinCrc);
            ViewBag.TotalMax = cart.Sum(x => x.SubtotalMaxCrc);
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

            TempData[CHECKOUT_DATA_KEY] = JsonSerializer.Serialize(data);

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
                // VALIDAR STOCK (solo EN STOCK)
                // ============================
                foreach (var item in cart.Where(i => i.Fulfillment == ProductRules.EnStock))
                {
                    var variant = _db.ProductVariants.FirstOrDefault(v => v.Id == item.VariantId);

                    if (variant == null || variant.Stock < item.Quantity)
                    {
                        TempData["Error"] = $"Stock insuficiente para {item.ProductName}.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                var totalMin = (int)cart.Sum(i => i.SubtotalMinCrc);
                var totalMax = (int)cart.Sum(i => i.SubtotalMaxCrc);

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

                    // si hay rango, guardamos el máximo como estimado
                    TotalCrc = totalMax,
                    Status = "Pendiente",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                _db.SaveChanges();

                // ============================
                // ITEMS + DESCONTAR STOCK (solo EN STOCK)
                // ============================
                foreach (var item in cart)
                {
                    var variant = _db.ProductVariants.FirstOrDefault(v => v.Id == item.VariantId);

                    if (variant != null && item.Fulfillment == ProductRules.EnStock)
                        variant.Stock -= item.Quantity;

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductName = item.ProductName,
                        Option = item.OptionValue ?? string.Empty,
                        Quantity = item.Quantity,
                        PriceCrc = item.UnitPriceCrc,
                        SubtotalCrc = item.UnitPriceCrc * item.Quantity
                    });
                }

                _db.SaveChanges();
                transaction.Commit();

                // ============================
                // ALERTA EMAIL AL DUEÑO (NO rompe checkout)
                // ============================
                _ = _alerts.TryNotifyNewOrderAsync(order.Id);
                // ============================
                // WHATSAPP
                // ============================
                var whatsappText = "Hola, realicé un pedido en Saint Hub.\n";
                whatsappText += $"Número de pedido: #{order.Id}\n\n";

                foreach (var item in cart)
                {
                    var opt = string.IsNullOrWhiteSpace(item.OptionValue)
                        ? ""
                        : $" ({item.OptionLabel}: {item.OptionValue})";

                    var priceText = item.IsPriceRange
                        ? $"₡{item.UnitPriceMinCrc:N0}–₡{item.UnitPriceMaxCrc:N0}"
                        : $"₡{item.UnitPriceCrc:N0}";

                    whatsappText += $"- {item.ProductName}{opt} x{item.Quantity} · {priceText}\n";
                }

                whatsappText += "\n";
                if (totalMin != totalMax)
                    whatsappText += $"Total estimado: ₡{totalMin:N0} – ₡{totalMax:N0}\n";
                else
                    whatsappText += $"Total: ₡{totalMax:N0}\n";

                whatsappText += "Quedo atento para coordinar pago y entrega.";

                TempData["WhatsappText"] = Uri.EscapeDataString(whatsappText);

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
        // CONFIRM (GET  BLOQUEO 405)
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
