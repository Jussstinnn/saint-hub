using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Services;

public class HomeContentController : Controller
{
    private readonly AppDbContext _db;

    public HomeContentController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Feature()
    {
        var feature = _db.HomeFeatureSections.FirstOrDefault()
                     ?? new HomeFeatureSection { IsActive = true };

        return View(feature);
    }

    [HttpPost]
    public async Task<IActionResult> Feature(HomeFeatureSection model, IFormFile image)
    {
        var feature = _db.HomeFeatureSections.FirstOrDefault();

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

            model.ImagePath = "/img/home/" + fileName;
        }
        else if (feature != null)
        {
            model.ImagePath = feature.ImagePath;
        }

        if (feature == null)
            _db.HomeFeatureSections.Add(model);
        else
            _db.Entry(feature).CurrentValues.SetValues(model);

        await _db.SaveChangesAsync();
        return RedirectToAction("Feature");
    }
}
