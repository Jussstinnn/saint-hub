namespace SaintHub.ViewModels
{
    public class VariantVM
    {
        // Razor expects `Id` in several places. Keep `VariantId` for clarity.
        public int Id
        {
            get => VariantId;
            set => VariantId = value;
        }

        public int VariantId { get; set; }
        public string Option { get; set; } = "";
        public int Stock { get; set; }
        public int PriceCrc { get; set; }
        public bool IsActive { get; set; }
    }
}
