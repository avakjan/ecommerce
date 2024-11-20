// Models/Size.cs
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSite.Models
{
    public class Size
    {
        public int SizeId { get; set; } // Primary key

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // e.g., "S", "M", "L", "40", "41"

        // Navigation property
        public ICollection<ItemSize> ItemSizes { get; set; }
    }
}