using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SaintHub.Models
{
    public class CustomRequest
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Phone { get; set; } = null!;

        public string? RequestType { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Size { get; set; }
        public string? Description { get; set; }

        [BindNever]
        public string Status { get; set; } = "Nuevo";

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}




