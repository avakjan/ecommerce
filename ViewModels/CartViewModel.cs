using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        [BindNever]
        public IEnumerable<Item>? Items { get; set; }
        [BindNever]
        public IEnumerable<Size>? Sizes { get; set; }
        public decimal TotalAmount 
        { 
            get 
            {
                decimal total = 0;
                if (CartItems != null && Items != null)
                {
                    foreach (var cartItem in CartItems)
                    {
                        var item = Items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                        if (item != null)
                        {
                            var itemSize = item.ItemSizes?.FirstOrDefault(isz => isz.SizeId == cartItem.SizeId);
                            if (itemSize != null)
                                total += item.Price * cartItem.Quantity;
                        }
                    }
                }
                return total;
            }
        }
    }
}
