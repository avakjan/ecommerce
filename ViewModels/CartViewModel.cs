using System.Collections.Generic;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public IEnumerable<Item> Items { get; set; }
        public IEnumerable<Size> Sizes { get; set; }

        // Optional: Add a property for Total Amount
        public decimal TotalAmount 
        { 
            get 
            {
                decimal total = 0;
                foreach (var cartItem in CartItems)
                {
                    var item = Items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                    if (item != null) {

                        var itemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == cartItem.SizeId);
                        if (itemSize != null)
                            total += item.Price * cartItem.Quantity;
                    }
                }
                return total;
            }
        }
    }
}
