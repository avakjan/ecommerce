// Models/Order.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OnlineShoppingSite.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Foreign key to ShippingDetails
        public int ShippingDetailsId { get; set; }
        public ShippingDetails ShippingDetails { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        // Collection of OrderItems
        [Required]
        public List<OrderItem> OrderItems { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentIntentId { get; set; } // Stores the Payment Intent ID

        [BindNever]
        public string UserId { get; set; } // Foreign key to ApplicationUser

        [BindNever]
        public ApplicationUser User { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";
    }
}