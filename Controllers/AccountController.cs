using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Filters;
using SaintHub.Models;
using SaintHub.ViewModels;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace SaintHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AccountController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }


        // =========================
        // LOGIN / REGISTER (LIBRES)
        // =========================

        public IActionResult Login() => View();
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user, string password)
        {
            user.Email = user.Email.Trim().ToLower();

            if (_db.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Correo ya registrado";
                return View();
            }

            user.PasswordHash = HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;

            _db.Users.Add(user);
            _db.SaveChanges();

            return RedirectToAction("Login");
        }
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback")
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            if (!result.Succeeded)
                return RedirectToAction("Login");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);
            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = _db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FullName = name,
                    GoogleId = googleId,
                    AuthProvider = "Google",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                _db.SaveChanges();
            }

            HttpContext.Session.SetString("USER_AUTH", user.Id.ToString());
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            email = email.Trim().ToLower();
            var user = _db.Users.FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                user.ResetToken = Guid.NewGuid().ToString();
                user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
                _db.SaveChanges();

                var link = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token = user.ResetToken },
                    Request.Scheme
                );

                SendResetEmail(user.Email, link);
            }

            // Mensaje genérico por seguridad
            ViewBag.Message = "Si el correo existe, recibirás un enlace para restablecer tu contraseña.";
            return View();
        }

        private void SendResetEmail(string to, string link)
        {
            var smtp = new SmtpClient(
                _config["Email:Host"],
                int.Parse(_config["Email:Port"])
            )
            {
                Credentials = new NetworkCredential(
                    _config["Email:User"],
                    _config["Email:Password"]
                ),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(_config["Email:User"], "Saint Hub"),
                Subject = "Restablecer contraseña - Saint Hub",
                Body = $"Hacé click en el siguiente enlace para crear una nueva contraseña:\n\n{link}\n\nEste enlace expira en 1 hora.",
                IsBodyHtml = false
            };

            message.To.Add(to);

            smtp.Send(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            email = email.Trim().ToLower();

            var user = _db.Users.FirstOrDefault(u => u.Email == email);

            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                // ?? Migrar hash viejo a nuevo si aplica
                if (!user.PasswordHash.Contains('.'))
                {
                    user.PasswordHash = HashPassword(password);
                    _db.SaveChanges();
                }

                HttpContext.Session.SetString("USER_AUTH", user.Id.ToString());
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Credenciales inválidas";
            return View();
        }



        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var user = _db.Users.FirstOrDefault(
                u => u.ResetToken == token &&
                     u.ResetTokenExpires > DateTime.UtcNow
            );

            if (user == null)
                return NotFound();

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string token, string password)
        {
            var user = _db.Users.FirstOrDefault(
                u => u.ResetToken == token &&
                     u.ResetTokenExpires > DateTime.UtcNow
            );

            if (user == null)
                return NotFound();

            user.PasswordHash = HashPassword(password);
            user.ResetToken = null;
            user.ResetTokenExpires = null;

            _db.SaveChanges();

            return RedirectToAction("Login");
        }

        // =========================
        // ZONA PROTEGIDA
        // =========================

        [AuthRequired]
        public IActionResult MyAccount()
        {
            return RedirectToAction("Dashboard");
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login");

            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            return View(user);
        }

        [AuthRequired]
        public IActionResult MyOrders()
        {
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login");

            var orders = _db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        [AuthRequired]
        public IActionResult OrderDetails(int id)
        {
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login");

            var order = _db.Orders
                .Include(o => o.Items) // ? ahora Model.Items sirve
                .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            return View(order);
        }
        [AuthRequired]
        public IActionResult Dashboard()
        {
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login");

            var user = _db.Users.First(u => u.Id == userId);

            var orders = _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var lastOrder = orders.FirstOrDefault();

            var vm = new AccountDashboardVM
            {
                FullName = user.FullName,
                TotalOrders = orders.Count,
                TotalSpentCrc = orders.Sum(o => o.TotalCrc),

                LastOrderId = lastOrder?.Id,
                LastOrderDate = lastOrder?.CreatedAt,
                LastOrderStatus = lastOrder?.Status
            };

            return View(vm);
        }
        [AuthRequired]
        [HttpGet]
        public IActionResult EditProfile()
        {
            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login");

            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return NotFound();

            var vm = new EditProfileVM
            {
                FullName = user.FullName,
                Email = user.Email
            };

            return View(vm);
        }

        [AuthRequired]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdStr = HttpContext.Session.GetString("USER_AUTH");
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login");

            var user = _db.Users.First(u => u.Id == userId);

            // =========================
            // VALIDAR EMAIL ÚNICO
            // =========================
            if (_db.Users.Any(u => u.Email == model.Email && u.Id != userId))
            {
                ModelState.AddModelError("", "Este correo ya está en uso.");
                return View(model);
            }

            // =========================
            // ACTUALIZAR DATOS
            // =========================
            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim().ToLower();

            // =========================
            // CAMBIO DE CONTRASEÑA (OPCIONAL)
            // =========================
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                // Debe ingresar la actual
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError("", "Ingresá tu contraseña actual para cambiarla.");
                    return View(model);
                }

                // Verificar contraseña actual
                if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("", "La contraseña actual es incorrecta.");
                    return View(model);
                }

                // Guardar nueva contraseña
                user.PasswordHash = HashPassword(model.NewPassword);
            }

            _db.SaveChanges();

            TempData["ProfileUpdated"] = "Perfil actualizado correctamente.";

            return RedirectToAction("Dashboard");
        }
        // =========================
        // LOGOUT
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // PASSWORD SECURITY
        // =========================

        private string HashPassword(string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                16,
                100_000,
                HashAlgorithmName.SHA256
            );

            var salt = deriveBytes.Salt;
            var key = deriveBytes.GetBytes(32);

            return Convert.ToBase64String(salt) + "." +
                   Convert.ToBase64String(key);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // HASH ANTIGUO (SHA256 simple)
            if (!storedHash.Contains('.'))
            {
                using var sha = SHA256.Create();
                var hash = Convert.ToBase64String(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(password))
                );

                return hash == storedHash;
            }

            // HASH NUEVO (PBKDF2)
            var parts = storedHash.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var key = Convert.FromBase64String(parts[1]);

            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256
            );

            var newKey = deriveBytes.GetBytes(32);
            return newKey.SequenceEqual(key);
        }

    }
}
