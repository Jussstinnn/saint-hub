using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Models;
using System.Security.Cryptography;
using System.Text;

namespace SaintHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) => _db = db;

        public IActionResult Login() => View();
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(User user, string password)
        {
            if (_db.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Correo ya registrado";
                return View();
            }

            user.PasswordHash = Hash(password);
            user.CreatedAt = DateTime.UtcNow;

            _db.Users.Add(user);
            _db.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var hash = Hash(password);
            var user = _db.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hash);

            if (user == null)
            {
                ViewBag.Error = "Credenciales inválidas";
                return View();
            }

            HttpContext.Session.SetString("USER_AUTH", user.Id.ToString());
            return RedirectToAction("Index", "Home");
        }
        public IActionResult MyAccount()
        {
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login");

            var userId = int.Parse(userIdStr);
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        public IActionResult MyOrders()
        {
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login");

            var userId = int.Parse(userIdStr);

            var orders = _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        public IActionResult OrderDetails(int id)
        {
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login");

            var userId = int.Parse(userIdStr);

            var order = _db.Orders.FirstOrDefault(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();

            var items = _db.OrderItems.Where(i => i.OrderId == id).ToList();
            ViewBag.Items = items;

            return View(order);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("USER_AUTH");
            return RedirectToAction("Home", "Index");
        }

        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}


