using System.ComponentModel.DataAnnotations;

namespace SaintHub.Models;

public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // (por ahora lo dejamos para no romper vistas/código)
    public string Option { get; set; } = "";

    // NUEVO: color de la variante
    [Required, StringLength(60)]
    public string Color { get; set; } = "Default";

    // NUEVO: precio por variante
    public int PriceCrc { get; set; }

    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

