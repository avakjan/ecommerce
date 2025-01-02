namespace OnlineShoppingSite.DTOs
{
    public class ItemDto
    {
        // Only the fields you want to return
        public int ItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
        public List<string> Sizes { get; set; } = new();
    }
}