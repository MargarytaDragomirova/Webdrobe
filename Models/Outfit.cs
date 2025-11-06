using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Outfit
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Store cloth IDs (e.g., [3, 5, 10]) as JSON text
        public string ClothIdsJson { get; set; } = string.Empty;

        // Not mapped property to easily access the IDs as a list
        [NotMapped]
        public List<int> ClothIds
        {
            get => string.IsNullOrEmpty(ClothIdsJson)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(ClothIdsJson)!;
            set => ClothIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}
