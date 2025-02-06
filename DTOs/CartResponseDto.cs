namespace OnlineShoppingSite.DTOs
{
    public class CartResponseDto
    {
        public List<CartItemDto> CartItems { get; set; }
        public List<ItemDto> Items { get; set; }
        public List<ItemSizeDto> Sizes { get; set; }
        public decimal TotalAmount { get; set; }
    }
}