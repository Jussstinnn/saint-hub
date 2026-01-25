using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Models;

public class AdminCustomRequestsController : Controller
{
    private readonly AppDbContext _context;

    public AdminCustomRequestsController(AppDbContext context)
    {
        _context = context;
    }

    // LISTADO
    public IActionResult Index()
    {
        var requests = _context.CustomRequests
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return View(requests);
    }




    // DETALLE
    public IActionResult Details(int id)
    {
        var request = _context.CustomRequests.FirstOrDefault(x => x.Id == id);
        if (request == null) return NotFound();

        return View(request);
    }

    // ACTUALIZAR ESTADO
    [HttpPost]
    public IActionResult UpdateStatus(int id, string status)
    {
        var request = _context.CustomRequests.FirstOrDefault(x => x.Id == id);
        if (request == null) return NotFound();

        request.Status = status;
        _context.SaveChanges();

        return RedirectToAction("Details", new { id });
    }
}



