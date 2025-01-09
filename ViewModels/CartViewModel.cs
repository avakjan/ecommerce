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
        public decimal TotalAmount { get; set; }
    }
}