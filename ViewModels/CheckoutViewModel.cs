// ViewModels/CheckoutViewModel.cs

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.ViewModels
{
    public class CheckoutViewModel
    {
        public ShippingDetails ShippingDetails { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        // Hidden field to store the PaymentIntent's Client Secret
        public string PaymentIntentClientSecret { get; set; }

        // Hidden field to store the PaymentIntent ID after confirmation
        [BindNever]
        public string? PaymentIntentId { get; set; }

        // Collection of Order Items
        public List<OrderItem>? OrderItems { get; set; }

        // Total Amount
        public decimal TotalAmount { get; set; }
    }
}
