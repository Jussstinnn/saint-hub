namespace SaintHub.ViewModels
{
    public class CartItemVM
    {
        public int ProductId { get; set; }
        public int VariantId { get; set; }

        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        public string Size { get; set; } = "";
        public int Quantity { get; set; }
        public int Stock { get; set; }

        public decimal PriceCrc { get; set; }
        public decimal SubtotalCrc => PriceCrc * Quantity;
    }
}


