using System.Collections.Generic;

namespace OnlineShoppingSite.Models
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public IEnumerable<Item> Items { get; set; }

        // Optional: Add a property for Total Amount
        public decimal TotalAmount 
        { 
            get 
            {
                decimal total = 0;
                foreach (var cartItem in CartItems)
                {
                    var item = Items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                    if (item != null)
                        total += item.Price * cartItem.Quantity;
                }
                return total;
            }
        }
    }
}
