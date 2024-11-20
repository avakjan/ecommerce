// Models/ItemSize.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShoppingSite.Models
{
    public class ItemSize
    {
        // Composite Key: ItemId + SizeId
        public int ItemId { get; set; }
        public Item Item { get; set; }

        public int SizeId { get; set; }
        public Size Size { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }

        // Version property for concurrency control
        public int Version { get; set; }
    }
}