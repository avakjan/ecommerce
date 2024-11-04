using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSite.Models
{
    public class Order
    {
        public int OrderId { get; set; } // Primary key

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Foreign key to ShippingDetails
        public int ShippingDetailsId { get; set; }
        public ShippingDetails ShippingDetails { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        // Collection of OrderItems
        public List<OrderItem> OrderItems { get; set; }

        public decimal TotalAmount { get; set; }

        public string ChargeId { get; set; }
    }
}