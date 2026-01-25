using SaintHub.Models;

namespace SaintHub.ViewModels
{
    public class AdminProductVariantsByColorVM
    {
        // INFO GENERAL
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PriceCrc { get; set; }
        public int Category { get; set; }
        public int Fulfillment { get; set; }
        public bool IsActive { get; set; }

        // On-demand pricing (Por encargo)
        public int? OnDemandFixedPriceCrc { get; set; }
        public int? OnDemandMinPriceCrc { get; set; }
        public int? OnDemandMaxPriceCrc { get; set; }

        // GALERÍA
        public List<ProductImage> Images { get; set; } = new();

        // VARIANTES PLANAS (legacy)
        public List<ProductVariant> Variants { get; set; } = new();

        // VARIANTES AGRUPADAS POR COLOR
        public List<ColorGroupVM> Colors { get; set; } = new();
    }
}
