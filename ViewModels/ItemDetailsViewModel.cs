// ViewModels/ItemDetailsViewModel.cs
using System.ComponentModel.DataAnnotations;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.ViewModels
{
    public class ItemDetailsViewModel
    {
        public Item Item { get; set; }

        [Required(ErrorMessage = "Please select a size.")]
        [Display(Name = "Size")]
        public int? SizeId { get; set; }

        [Required(ErrorMessage = "Please enter a quantity.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;
    }
}