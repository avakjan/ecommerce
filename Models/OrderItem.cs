namespace OnlineShoppingSite.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; } // Primary key

        // Foreign keys
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }
        public int? SizeId { get; set; } // Foreign key to Size, nullable for items without sizes
        public Size Size { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}