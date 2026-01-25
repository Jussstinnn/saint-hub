using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;

namespace SaintHub.Controllers
{
    public class AdminOrdersController : Controller
    {
        private readonly AppDbContext _db;

        public AdminOrdersController(AppDbContext db)
        {
            _db = db;
        }

        // LISTA
        public IActionResult Index()
        {
            var orders = _db.Orders
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        // EDIT / VER
        public IActionResult Edit(int id)
        {
            var order = _db.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // CAMBIAR ESTADO
        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            order.Status = status;
            _db.SaveChanges();

            return RedirectToAction("Edit", new { id });
        }
    }
}



