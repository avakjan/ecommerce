// ViewModels/CheckoutViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.ViewModels
{
    public class CheckoutViewModel
    {
        // Shipping Details
        public ShippingDetails ShippingDetails { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        // Payment Intent Client Secret (hidden field)
        public string PaymentIntentClientSecret { get; set; }

        // Payment Intent ID (hidden field)
        public string PaymentIntentId { get; set; }

        // Order Summary
        [BindNever]
        public List<OrderItem>? OrderItems { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
