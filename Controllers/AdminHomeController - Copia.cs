using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Models;
using SaintHub.Services;

public class AdminHomeController : Controller
{
    private readonly AppDbContext _db;

    public AdminHomeController(AppDbContext db)
    {
        _db = db;
    }

    // GET
    public IActionResult Feature()
    {
        var feature = _db.HomeFeatureSections.FirstOrDefault()
                      ?? new HomeFeatureSection { IsActive = true };

        return View(feature);
    }

    // POST
    [HttpPost]
    public async Task<IActionResult> Feature(HomeFeatureSection model, IFormFile image)
    {
        var feature = _db.HomeFeatureSections.FirstOrDefault();

        if (feature == null)
        {
            feature = new HomeFeatureSection();
            _db.HomeFeatureSections.Add(feature);
        }

        // Imagen
        if (image != null)
        {
            // Seguridad + soporte .webp/.avif
            if (!ImageUploadRules.IsAllowed(image))
            {
                TempData["Error"] = "Formato de imagen no permitido. Permitidos: JPG, PNG, GIF, WEBP, AVIF.";
                return RedirectToAction(nameof(Feature));
            }

            var fileName = $"feature-{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            Directory.CreateDirectory(Path.Combine("wwwroot", "img", "home"));
            var path = Path.Combine("wwwroot", "img", "home", fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await image.CopyToAsync(stream);

            feature.ImagePath = "/img/home/" + fileName;
        }

        // Campos editables (UNO POR UNO)
        feature.SmallTitle = model.SmallTitle;
        feature.Title = model.Title;
        feature.Description = model.Description;
        feature.ButtonText = model.ButtonText;
        feature.ButtonUrl = model.ButtonUrl;
        feature.IsActive = model.IsActive;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Feature));
    }

}
