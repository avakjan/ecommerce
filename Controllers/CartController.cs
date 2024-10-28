using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace OnlineShoppingSite.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            // Retrieve cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        [HttpPost]
        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.Item.ItemId == id);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            // Retrieve cart
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Logic to process the order (e.g., save to database)

            // Clear the cart
            HttpContext.Session.Remove("Cart");

            return View();
        }


        // Additional actions to update quantities, etc.
    }
}