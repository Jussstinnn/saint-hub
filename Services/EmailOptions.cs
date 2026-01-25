namespace SaintHub.Services
{
    public class EmailOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableSsl { get; set; } = true;

        // Opcional: si no lo ponés, usa "User" como From
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
    }
}
