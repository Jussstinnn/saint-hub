using Microsoft.AspNetCore.Mvc;
using SaintHub.Data;
using SaintHub.Models;
using System.Net;
using System.Text;

namespace SaintHub.Controllers
{
    public class CustomOrderController : Controller
    {
        private readonly AppDbContext _context;

        public CustomOrderController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new CustomRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(CustomRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);

            request.Status = "Nuevo";
            request.CreatedAt = DateTime.Now;

            _context.CustomRequests.Add(request);
            _context.SaveChanges();

            return RedirectToAction("Success", new { id = request.Id });
        }





        public IActionResult Success(int id)
        {
            var request = _context.CustomRequests.FirstOrDefault(x => x.Id == id);
            if (request == null) return RedirectToAction("Index");

            // Construimos el mensaje "normal" (con saltos reales)
            var sb = new StringBuilder();
            sb.AppendLine("Hola Saint Hub, realicé una solicitud de pedido personalizado.");
            sb.AppendLine();
            sb.AppendLine($"Solicitud: #{request.Id}");
            sb.AppendLine($"Tipo: {request.ProductType}");

            if (!string.IsNullOrWhiteSpace(request.Brand))
                sb.AppendLine($"Marca: {request.Brand}");

            if (!string.IsNullOrWhiteSpace(request.ProductName))
                sb.AppendLine($"Producto: {request.ProductName}");

            if (!string.IsNullOrWhiteSpace(request.Size))
                sb.AppendLine($"Talla (EU): {request.Size}");

            if (!string.IsNullOrWhiteSpace(request.GenderModel))
                sb.AppendLine($"Modelo: {request.GenderModel}");

            if (!string.IsNullOrWhiteSpace(request.Color))
                sb.AppendLine($"Color: {request.Color}");

            if (!string.IsNullOrWhiteSpace(request.ReferenceLink))
                sb.AppendLine($"Referencia: {request.ReferenceLink}");

            if (!string.IsNullOrWhiteSpace(request.Comments))
            {
                sb.AppendLine();
                sb.AppendLine("Comentarios:");
                sb.AppendLine(request.Comments);
            }

            // ? URL Encode real (esto evita ?? y cortes)
            var encoded = WebUtility.UrlEncode(sb.ToString());

            ViewBag.RequestId = request.Id;
            ViewBag.WhatsappMessage = encoded;

            return View();
        }


    }
}



