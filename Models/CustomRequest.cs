using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace SaintHub.Models
{
    public class CustomRequest
    {
        public int Id { get; set; }

        // =========================
        // DATOS DE CONTACTO
        // =========================
        [Required]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Phone { get; set; } = null!;

        // =========================
        // DATOS DEL PRODUCTO
        // =========================
        [Required]
        public string ProductType { get; set; } = null!; // Tenis / Ropa / Accesorios / Música

        public string? Brand { get; set; }
        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? GenderModel { get; set; }
        public string? Color { get; set; }
        public string? ReferenceLink { get; set; }
        public string? Comments { get; set; }

        // =========================
        // SISTEMA
        // =========================
        [BindNever]
        public string Status { get; set; } = "Nuevo";

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
