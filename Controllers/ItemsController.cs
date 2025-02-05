// Controllers/ItemsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.Extensions;
using OnlineShoppingSite.ViewModels;
using OnlineShoppingSite.Models.Requests;

namespace OnlineShoppingSite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
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
        [HttpGet]
        public async Task<IActionResult> GetItems([FromQuery] int categoryId = 0)
        {
            // Try to get the cached categories
            if (!_cache.TryGetValue("Categories", out var categories))
            {
                categories = await _context.Categories.ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(60));

                _cache.Set("Categories", categories, cacheEntryOptions);
            }

            // Build the query for items
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

            // Return a JSON response with the data
            return Ok(new
            {
                items,
                categories,
                selectedCategoryId = categoryId
            });
        }

        // GET: api/items/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemDetails(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details: Item ID is null.");
                return NotFound(new { message = "Item ID is required." });
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemSizes)
                    .ThenInclude(isz => isz.Size)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (item == null)
            {
                _logger.LogWarning("Details: Item not found with ID {ItemId}.", id);
                return NotFound(new { message = $"Item not found with ID {id}." });
            }

            var viewModel = new ItemDetailsViewModel
            {
                Item = item,
                SizeId = null, // Default selection
                Quantity = 1
            };

            return Ok(viewModel);
        }

        // POST: api/items/addToCart
        [HttpPost("addToCart")]
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            // Validate the quantity parameter
            if (request.Quantity < 1)
            {
                _logger.LogWarning("AddToCart: Quantity must be at least 1. ItemId: {ItemId}", request.ItemId);
                return BadRequest(new { message = "Quantity must be at least 1." });
            }

            // Fetch the item size from the database to ensure the item and size exist and have sufficient stock
            var itemSize = _context.ItemSizes
                .FirstOrDefault(isz => isz.ItemId == request.ItemId && isz.SizeId == request.SizeId);

            if (itemSize == null)
            {
                _logger.LogWarning("AddToCart: Selected size is not available for ItemId {ItemId}, SizeId {SizeId}.", request.ItemId, request.SizeId);
                return NotFound(new { message = "Selected size is not available for this item." });
            }

            if (itemSize.Quantity < request.Quantity)
            {
                _logger.LogWarning("AddToCart: Out of stock for ItemId {ItemId}, SizeId {SizeId}.", request.ItemId, request.SizeId);
                return BadRequest(new { message = "This size is out of stock." });
            }

            // Retrieve the cart from the session (or create a new one if it doesn't exist)
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Check if the item with the same size is already in the cart
            var cartItem = cart.FirstOrDefault(c => c.ItemId == request.ItemId && c.SizeId == request.SizeId);
            if (cartItem != null)
            {
                // Increase the quantity for the existing cart item
                cartItem.Quantity += request.Quantity;
                _logger.LogInformation("AddToCart: Increased quantity for ItemId {ItemId} SizeId {SizeId} by {Quantity}.", request.ItemId, request.SizeId, request.Quantity);
            }
            else
            {
                // Add a new item to the cart
                cart.Add(new CartItem { ItemId = request.ItemId, SizeId = request.SizeId, Quantity = request.Quantity });
                _logger.LogInformation("AddToCart: Added ItemId {ItemId} SizeId {SizeId} to cart with quantity {Quantity}.", request.ItemId, request.SizeId, request.Quantity);
            }

            // Save the updated cart back to the session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Return a success message and optionally the updated cart
            return Ok(new 
            { 
                message = "Item added to cart successfully.", 
                cart 
            });
        }
    }
}