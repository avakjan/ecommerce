// Models/Requests/AddToCartRequest.cs
namespace OnlineShoppingSite.Models.Requests
{
    public class AddToCartRequest
    {
        public int ItemId { get; set; }
        public int SizeId { get; set; }
        public int Quantity { get; set; }
    }
}
