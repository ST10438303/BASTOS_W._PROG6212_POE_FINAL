namespace RaceDay.Api.Services
{
    /// <summary>
    /// Validates uploaded images before they are sent to Blob Storage: size limit,
    /// allowed type, and a check of the file's leading bytes so a renamed non-image
    /// file is rejected.
    /// </summary>
    public static class ImageUploadValidator
    {
        public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        public sealed record ValidImage(string ContentType, string Extension);

        /// <returns>The detected image type, or null with an error message when invalid.</returns>
        public static async Task<(ValidImage? Image, string? Error)> ValidateAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (null, "Please choose an image file to upload.");
            }

            if (file.Length > MaxBytes)
            {
                return (null, "The image is too large. The maximum size is 5 MB.");
            }

            var header = new byte[12];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
                if (read < header.Length)
                {
                    return (null, "The file is not a valid image.");
                }
            }

            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return (new ValidImage("image/jpeg", ".jpg"), null);
            }

            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            {
                return (new ValidImage("image/png", ".png"), null);
            }

            // WEBP: "RIFF" .... "WEBP"
            if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            {
                return (new ValidImage("image/webp", ".webp"), null);
            }

            return (null, "Only JPG, PNG or WEBP images are allowed.");
        }
    }
}
