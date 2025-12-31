using System.ComponentModel.DataAnnotations;

namespace SaintHub.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User User { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string Address { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string? Notes { get; set; }

        public int TotalCrc { get; set; }
        public string Status { get; set; } = "Pendiente";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderItem> Items { get; set; } = new();
    }
}
