// Controllers/ItemsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OnlineShoppingSite.Extensions;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ItemsController> _logger;
        private readonly IMemoryCache _cache;

        public ItemsController(ApplicationDbContext context, ILogger<ItemsController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        // GET: Items
        public async Task<IActionResult> Index(int categoryId = 0)
        {
            var categories = await _context.Categories.ToListAsync();
            if (!_cache.TryGetValue("Categories", out categories))
            {
                categories = await _context.Categories.ToListAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(60));

                _cache.Set("Categories", categories, cacheEntryOptions);
            }
            var itemsQuery = _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemSizes)
                    .ThenInclude(isz => isz.Size)
                .AsQueryable();

            if (categoryId != 0)
            {
                itemsQuery = itemsQuery.Where(i => i.CategoryId == categoryId);
            }

            var items = await itemsQuery.ToListAsync();

            // Fetch categories for the filter dropdown
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;

            return View(items);
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
                .Include(i => i.ItemSizes)
                    .ThenInclude(isz => isz.Size)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (item == null)
            {
                _logger.LogWarning("Details: Item not found with ID {ItemId}.", id);
                return NotFound();
            }

            var viewModel = new ItemDetailsViewModel
            {
                Item = item,
                SizeId = null, // Default selection
                Quantity = 1
            };

            return View(viewModel);
        }

        // POST: Items/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int itemId, int sizeId, int quantity)
        {
            if (quantity < 1)
            {
                TempData["Error"] = "Quantity must be at least 1.";
                return RedirectToAction("Details", new { id = itemId });
            }

            // Fetch the item and size to ensure they exist and have sufficient stock
            var itemSize = _context.ItemSizes
                .FirstOrDefault(isz => isz.ItemId == itemId && isz.SizeId == sizeId);

            if (itemSize == null)
            {
                TempData["Error"] = "Selected size is not available for this item.";
                return RedirectToAction("Details", new { id = itemId });
            }

            if (itemSize.Quantity < quantity)
            {
                TempData["Error"] = "Insufficient stock for the selected quantity.";
                return RedirectToAction("Details", new { id = itemId });
            }

            // Retrieve the cart from the session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Check if item with the same size is already in cart
            var cartItem = cart.FirstOrDefault(c => c.ItemId == itemId && c.SizeId == sizeId);
            if (cartItem != null)
            {
                // Increase quantity
                cartItem.Quantity += quantity;
                _logger.LogInformation("Increased quantity of ItemId {ItemId} SizeId {SizeId} in cart by {Quantity}.", itemId, sizeId, quantity);
            }
            else
            {
                // Add new cart item
                cart.Add(new CartItem { ItemId = itemId, SizeId = sizeId, Quantity = quantity });
                _logger.LogInformation("Added ItemId {ItemId} SizeId {SizeId} to cart with quantity {Quantity}.", itemId, sizeId, quantity);
            }

            // Save cart back to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Add a success message
            TempData["Success"] = "Item added to cart successfully.";

            // Redirect back to the item details page
            return RedirectToAction("Details", new { id = itemId });
        }
    }
}