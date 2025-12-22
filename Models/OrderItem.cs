namespace SaintHub.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string ProductName { get; set; } = string.Empty;

        // ?? ESTA ES LA CLAVE (talla / variante)
        public string Option { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal PriceCrc { get; set; }

        public decimal SubtotalCrc { get; set; }
    }

}

