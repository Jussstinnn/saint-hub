namespace SaintHub.ViewModels
{
    public class CartItemVM
    {
        public int ProductId { get; set; }
        public int VariantId { get; set; }

        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        // Meta (for display rules)
        public int Category { get; set; }
        public int Fulfillment { get; set; }

        // Option display ("Talla" / "Opción" / etc.)
        public string OptionLabel { get; set; } = "Talla";
        public string OptionValue { get; set; } = "";

        public int Quantity { get; set; }
        public int Stock { get; set; }

        // Pricing
        public decimal UnitPriceCrc { get; set; }
        public int? UnitPriceMinCrc { get; set; }
        public int? UnitPriceMaxCrc { get; set; }

        public bool IsPriceRange => UnitPriceMinCrc.HasValue && UnitPriceMaxCrc.HasValue && UnitPriceMinCrc.Value != UnitPriceMaxCrc.Value;

        public decimal SubtotalCrc => UnitPriceCrc * Quantity;
        public decimal SubtotalMinCrc => (UnitPriceMinCrc ?? (int)UnitPriceCrc) * Quantity;
        public decimal SubtotalMaxCrc => (UnitPriceMaxCrc ?? (int)UnitPriceCrc) * Quantity;
    }
}
