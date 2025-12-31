namespace SaintHub.ViewModels
{
    public class AccountDashboardVM
    {
        public string FullName { get; set; } = "";

        public int TotalOrders { get; set; }
        public int TotalSpentCrc { get; set; }

        public int? LastOrderId { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public string? LastOrderStatus { get; set; }
    }
}
