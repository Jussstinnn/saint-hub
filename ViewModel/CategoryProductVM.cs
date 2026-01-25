namespace SaintHub.ViewModels
{
    public class CategoryProductVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public int Category { get; set; }
        public int Fulfillment { get; set; }

        // Display pricing
        public int PriceFromCrc { get; set; }
        public int PriceToCrc { get; set; }
        public bool IsPriceRange { get; set; }
        public string? PriceNote { get; set; }

        // Legacy (kept for compatibility)
        public int PriceCrc { get; set; }

        // Images
        public List<string> Images { get; set; } = new();
    }
}
