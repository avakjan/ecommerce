using System.Collections.Generic;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.ViewModels
{
    public class ItemsViewModel
    {
        public List<Item> Items { get; set; }
        public List<Category> Categories { get; set; }
        public int SelectedCategoryId { get; set; }
    }
}
