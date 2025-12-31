using System.ComponentModel.DataAnnotations;

namespace SaintHub.ViewModels
{
    public class EditProfileVM
    {
        // ===== DATOS =====
        [Required]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        // ===== CONTRASEÑA =====
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmNewPassword { get; set; }
    }
}
