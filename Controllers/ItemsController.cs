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
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(ApplicationDbContext context, ILogger<ItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Items
        public IActionResult Index()
        {
            var items = _context.Items.ToList();
            return View(items);
        }

        // POST: Items/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int id)
        {
            // Retrieve the item by id
            var item = _context.Items.FirstOrDefault(i => i.ItemId == id);
            if (item == null)
            {
                _logger.LogWarning("AddToCart: Item with ID {ItemId} not found.", id);
                return NotFound();
            }

            // Retrieve cart from session or create a new one
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Check if item is already in cart
            var cartItem = cart.FirstOrDefault(c => c.ItemId == id);
            if (cartItem != null)
            {
                // Increase quantity
                cartItem.Quantity++;
                _logger.LogInformation("Increased quantity of ItemId {ItemId} in cart.", id);
            }
            else
            {
                // Add new cart item
                cart.Add(new CartItem { ItemId = id, Quantity = 1 });
                _logger.LogInformation("Added ItemId {ItemId} to cart.", id);
            }

            // Save cart back to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Optionally, add a success message
            TempData["Success"] = $"{item.Name} has been added to your cart.";

            // Redirect back to the items index page
            return RedirectToAction("Index");
        }


        // Other action methods...
    }
}