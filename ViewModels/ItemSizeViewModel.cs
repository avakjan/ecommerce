// ViewModels/ItemSizeViewModel.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineShoppingSite.Models;
using System.Collections.Generic;

namespace OnlineShoppingSite.ViewModels
{
    public class ItemSizeViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }

        public List<ItemSizeAssignment> SizeAssignments { get; set; }

        public class ItemSizeAssignment
        {
            public int SizeId { get; set; }
            public string SizeName { get; set; }
            public int Quantity { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}