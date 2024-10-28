namespace OnlineShoppingSite.Models
{
    public class Item
    {
        public int ItemId { get; set; } // Primary key
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; } // Ensure Description exists
        public string ImageUrl { get; set; }     // Add this property
    }
}
