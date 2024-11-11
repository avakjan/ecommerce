using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> Index(int? categoryId)
        {
            var items = _context.Items.Include(i => i.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                items = items.Where(i => i.CategoryId == categoryId.Value);
                ViewBag.SelectedCategoryId = categoryId.Value;
            }
            else
            {
                ViewBag.SelectedCategoryId = 0; // All categories
            }

            var itemList = await items.ToListAsync();
            return View(itemList);
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details: Item ID is null.");
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (item == null)
            {
                _logger.LogWarning("Details: Item not found with ID {ItemId}.", id);
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int id, int quantity = 1)
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
                cartItem.Quantity += quantity;
                _logger.LogInformation("Increased quantity of ItemId {ItemId} in cart by {Quantity}.", id, quantity);
            }
            else
            {
                // Add new cart item
                cart.Add(new CartItem { ItemId = id, Quantity = quantity });
                _logger.LogInformation("Added ItemId {ItemId} to cart with quantity {Quantity}.", id, quantity);
            }

            // Save cart back to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Add a success message
            TempData["Success"] = $"{item.Name} (Quantity: {quantity}) has been added to your cart.";

            // Redirect back to the item details page or the index
            return RedirectToAction("Index");
        }
    }
}