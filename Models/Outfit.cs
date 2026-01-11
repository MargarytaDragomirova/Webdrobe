using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Outfit
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string ClothIdsJson { get; set; } = string.Empty;

        [NotMapped]
        public List<int> ClothIds
        {
            get => string.IsNullOrEmpty(ClothIdsJson)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(ClothIdsJson)!;
            set => ClothIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        public List<Cloth>? Cloths {get;set;} = [];
    }
}
