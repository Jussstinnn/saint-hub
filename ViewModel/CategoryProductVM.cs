namespace SaintHub.ViewModels
{
    public class CategoryProductVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int PriceCrc { get; set; }
        public string? ImageUrl { get; set; }
        public int Fulfillment { get; set; }
        // ?? NUEVO
        public List<string> Images { get; set; } = new();

        // (opcional, la primera)
        
    }
}


