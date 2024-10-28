using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSite.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }
        //public List<CartItem> Items { get; set; }
    }
}