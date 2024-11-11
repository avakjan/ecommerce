// Models/Category.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OnlineShoppingSite.Models
{
    public class Category
    {
        public int CategoryId { get; set; } // Primary key

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // Navigation property
        [ValidateNever]
        public ICollection<Item> Items { get; set; }
    }
}