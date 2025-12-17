using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaintHub.Models;

public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Option { get; set; } = "";
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

