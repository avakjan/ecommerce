// Models/Item.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OnlineShoppingSite.Models
{
    public class Item
    {
        public Item()
        {
            ItemSizes = new List<ItemSize>();
        }

        public int ItemId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        public decimal Price { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public int? CategoryId { get; set; }
        
        [ValidateNever]
        public Category Category { get; set; }

        public ICollection<ItemSize> ItemSizes { get; set; }
    }
}