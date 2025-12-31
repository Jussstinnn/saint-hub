namespace SaintHub.ViewModels
{
    public class ColorGroupVM
    {
        public string Color { get; set; }
        public List<VariantVM> Variants { get; set; } = new();
    }
}
