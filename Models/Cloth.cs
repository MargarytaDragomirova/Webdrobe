using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace WebApplication1.Models
{
    public class Cloth
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;

        public string? Location { get; set; }  // ✅ New field for cloth location (e.g. "Wardrobe", "Drawer")

        public string? UserId { get; set; }
        public string? ImagePathsJson { get; set; }

        [NotMapped]
        public List<string> ImagePaths
        {
            get => string.IsNullOrEmpty(ImagePathsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(ImagePathsJson)!;
            set => ImagePathsJson = JsonSerializer.Serialize(value);
        }
    }
}
