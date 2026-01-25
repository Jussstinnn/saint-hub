namespace SaintHub.ViewModels
{
    public class UpdateProductInfoVM
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public int PriceCrc { get; set; }
        public int Category { get; set; }
        public int Fulfillment { get; set; }
        public bool IsActive { get; set; }

        // On-demand pricing (Por encargo)
        public string? OnDemandMode { get; set; } // fixed | range
        public int? OnDemandFixedPriceCrc { get; set; }
        public int? OnDemandMinPriceCrc { get; set; }
        public int? OnDemandMaxPriceCrc { get; set; }
    }
}
