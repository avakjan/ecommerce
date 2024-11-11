using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.ViewModels
{
    public class ItemViewModel
    {
        public Item Item { get; set; }
        [ValidateNever]
        public SelectList Categories { get; set; }
    }
}
