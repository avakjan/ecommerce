namespace OnlineShoppingSite.DTOs
{
    public class ItemDto
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public CategoryDto Category { get; set; }
        public List<ItemSizeDto> ItemSizes { get; set; }
    }
}