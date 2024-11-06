using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSite.Models
{
    public class ShippingDetails
    {
        public int ShippingDetailsId { get; set; } // Primary key

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required.")]
        [StringLength(200, ErrorMessage = "Address Line 1 cannot exceed 200 characters.")]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; }

        [StringLength(200, ErrorMessage = "Address Line 2 cannot exceed 200 characters.")]
        [Display(Name = "Address Line 2")]
        public string AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string City { get; set; }

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        public string State { get; set; }

        [Required(ErrorMessage = "Postal Code is required.")]
        [StringLength(20, ErrorMessage = "Postal Code cannot exceed 20 characters.")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}