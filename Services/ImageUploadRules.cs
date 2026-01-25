using Microsoft.AspNetCore.Http;

namespace SaintHub.Services
{
    /// <summary>
    /// Centralized image upload rules (used by Admin/Home uploads).
    /// Allows: jpg/jpeg, png, gif, webp, avif.
    /// </summary>
    public static class ImageUploadRules
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif"
        };

        // Some browsers/clients might send empty or generic content-types.
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp",
            "image/avif"
        };

        public static bool IsAllowed(IFormFile? file)
        {
            if (file == null) return false;
            if (file.Length <= 0) return false;

            var ext = Path.GetExtension(file.FileName ?? "") ?? "";
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
                return false;

            // If ContentType is present, validate it too.
            var ct = (file.ContentType ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(ct) && !AllowedContentTypes.Contains(ct))
                return false;

            return true;
        }

        public static string AcceptAttribute => "image/jpeg,image/png,image/gif,image/webp,image/avif,.webp,.avif";
    }
}
