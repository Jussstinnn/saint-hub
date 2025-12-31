using System.ComponentModel.DataAnnotations;

namespace SaintHub.Models;

public class ProductImage
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public string Url { get; set; } = "";
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public string? Color { get; set; }

}
