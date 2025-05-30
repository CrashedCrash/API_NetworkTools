// API_NetworkTools/Tools/Models/ShortUrlMapping.cs
using System.ComponentModel.DataAnnotations;

namespace API_NetworkTools.Models
{
    public class ShortUrlMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public required string ShortCode { get; set; }

        [Required]
        public required string LongUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ClickCount { get; set; } = 0;
    }
}