public class HomeFeatureSection
{
    public int Id { get; set; }

    public string SmallTitle { get; set; }   // "Lo más pedido"
    public string Title { get; set; }        // "Timberland 6 Inch"

    public string Description { get; set; }  // Texto largo
    public string ImagePath { get; set; }    // /img/home/feature-1.jpg

    public string ButtonText { get; set; }   // "Ver colección"
    public string ButtonUrl { get; set; }    // /Shop

    public bool IsActive { get; set; }        // Mostrar / ocultar
}
