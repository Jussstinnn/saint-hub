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
        [Required]
        public string Address { get; set; } = null!;
        // =========================
        // MÚSICA
        // =========================
        public string? MusicArtist { get; set; }
        public string? AlbumName { get; set; }
        public string? Edition { get; set; }        // Deluxe, Remastered, etc.
        public string? MusicFormat { get; set; }    // LP / EP / CD
        public string? Condition { get; set; }      // Nuevo / Usado (VG+, VG...)
                                                    // =========================
                                                    // ACCESORIOS
                                                    // =========================
        public string? AccessoryType { get; set; }      // Faja, Cadena, Gorra, Otro
        public string? AccessoryOther { get; set; }     // Si elige "Otro"
        public string? Measurements { get; set; }       // Talla / Largo / etc.
        public string? Material { get; set; }
        public string? Style { get; set; }

        // =========================
        // SISTEMA
        // =========================
        [BindNever]
        public string Status { get; set; } = "Nuevo";

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
