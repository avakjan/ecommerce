using System;

namespace OnlineShoppingSite.Models
{
    public class AddToCartModel
    {
        public int ItemId { get; set; }
        public int? SizeId { get; set; }
        public int Quantity { get; set; }
    }
}
