// API_NetworkTools/Tools/Models/ShortUrlMapping.cs (oder Data/Models/)
using System.ComponentModel.DataAnnotations;

namespace API_NetworkTools.Models // Oder API_NetworkTools.Data.Models
{
    public class ShortUrlMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)] // Länge des Kurzcodes, anpassbar
        public required string ShortCode { get; set; }

        [Required]
        public required string LongUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ClickCount { get; set; } = 0; // Optional: Klicks zählen
    }
}