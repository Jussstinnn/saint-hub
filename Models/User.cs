using System.ComponentModel.DataAnnotations;

namespace SaintHub.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string? GoogleId { get; set; }
        public string AuthProvider { get; set; } = "Local"; // Local | Google
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
