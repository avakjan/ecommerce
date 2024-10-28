using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSite.Extensions;
using OnlineShoppingSite.Models;
using System.Collections.Generic;
using System.Linq;

namespace OnlineShoppingSite.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Items
        public IActionResult Index()
        {
            var items = _context.Items.ToList();
            return View(items);
        }

        // POST: Items/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            // Retrieve the item by id
            var item = _context.Items.FirstOrDefault(i => i.ItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            // Retrieve cart from session or create a new one
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Check if item is already in cart
            var cartItem = cart.FirstOrDefault(c => c.Item.ItemId == id);
            if (cartItem != null)
            {
                // Increase quantity
                cartItem.Quantity++;
            }
            else
            {
                // Add new cart item
                cart.Add(new CartItem { Item = item, Quantity = 1 });
            }

            // Save cart back to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Redirect back to the items index page
            return RedirectToAction("Index");
        }

        // Other action methods...
    }
}