namespace OnlineShoppingSite.Models
{
    public class CartItem
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public int SizeId { get; set; } // Foreign key to Size
    }
}