using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaintHub.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public int PriceCrc { get; set; }

    public int Category { get; set; }
    public int Fulfillment { get; set; }
    public int Source { get; set; }

    [Column("Condition")]
    public int Condition { get; set; }

    public string? SourceUrl { get; set; }

    public int? LeadTimeDaysMin { get; set; }
    public int? LeadTimeDaysMax { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ProductImage> Images { get; set; } = new();
    public List<ProductVariant> Variants { get; set; } = new();
}


