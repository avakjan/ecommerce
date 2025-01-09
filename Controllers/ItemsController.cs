using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OnlineShoppingSite.Extensions;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using OnlineShoppingSite.DTOs; // <<==== Note: import DTOs namespace
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ItemsController> _logger;
        private readonly IMemoryCache _cache;

        public ItemsController(
            ApplicationDbContext context,
            ILogger<ItemsController> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Gets a list of items (as ItemDto), optionally filtered by category.
        /// Also returns cached categories if desired by the front end.
        /// </summary>
        /// <param name="categoryId">If non-zero, filters items by this category.</param>
        [HttpGet]
        public async Task<IActionResult> GetItems([FromQuery] int categoryId = 0)
        {
            // Try getting categories from cache
            if (!_cache.TryGetValue("Categories", out List<Category> categories))
            {
                categories = await _context.Categories.ToListAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(60));

                _cache.Set("Categories", categories, cacheEntryOptions);
            }

            // Build the query
            var itemsQuery = _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemSizes)
                    .ThenInclude(isz => isz.Size)
                .AsQueryable();

            if (categoryId != 0)
            {
                itemsQuery = itemsQuery.Where(i => i.CategoryId == categoryId);
            }

            // Project EF entities -> ItemDto
            var itemDtos = await itemsQuery
                .Select(item => new ItemDto
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Description = item.Description,
                    ImageUrl = item.ImageUrl,
                    CategoryName = item.Category != null ? item.Category.Name : null,
                    Sizes = item.ItemSizes
                        .Select(isz => isz.Size.Name) // e.g. "S", "M", "L", etc.
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                Items = itemDtos,
                Categories = categories,
                SelectedCategoryId = categoryId
            });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            if (!_cache.TryGetValue("CategoriesList", out List<Category> categories))
            {
                categories = await _context.Categories.ToListAsync();
                _cache.Set("CategoriesList", categories, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }
            return Ok(categories);
        }

        /// <summary>
        /// Retrieves details for a specific item by ID, but returns it as an ItemDto.
        /// </summary>
        /// <param name="id">Item ID.</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemDetails(int id)
        {
            // Eager load related data for constructing the DTO
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemSizes)
                    .ThenInclude(isz => isz.Size)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (item == null)
            {
                _logger.LogWarning("GetItemDetails: Item not found with ID {ItemId}.", id);
                return NotFound(new { Error = "Item not found." });
            }

            // Convert to ItemDto
        var itemDto = new
        {
            itemId = item.ItemId,
            name = item.Name,
            description = item.Description,
            imageUrl = item.ImageUrl,
            price = item.Price,
            categoryName = item.Category?.Name,
            sizes = item.ItemSizes.Select(isz => new
            {
                sizeId = isz.SizeId,
                name = isz.Size.Name
            }).ToList()
        };

            return Ok(itemDto);
        }

        /// <summary>
        /// Adds an item to the cart by itemId, sizeId, and quantity.
        /// </summary>
        [HttpPost("addToCart")]
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            // Validate
            if (request.Quantity < 1)
            {
                return BadRequest(new { Error = "Quantity must be at least 1." });
            }

            var itemSize = _context.ItemSizes
                .FirstOrDefault(isz => isz.ItemId == request.ItemId && isz.SizeId == request.SizeId);

            if (itemSize == null)
            {
                return BadRequest(new { Error = "Selected size is not available for this item." });
            }

            if (itemSize.Quantity < request.Quantity)
            {
                return BadRequest(new { Error = "This size is out of stock or does not have enough quantity." });
            }

            // Retrieve or create cart in session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") 
                       ?? new List<CartItem>();

            // Find existing item in cart
            var cartItem = cart.FirstOrDefault(c => c.ItemId == request.ItemId && c.SizeId == request.SizeId);
            if (cartItem != null)
            {
                cartItem.Quantity += request.Quantity;
                _logger.LogInformation("Increased quantity of ItemId {ItemId} SizeId {SizeId} in cart by {Quantity}.",
                    request.ItemId, request.SizeId, request.Quantity);
            }
            else
            {
                cart.Add(new CartItem
                {
                    ItemId = request.ItemId,
                    SizeId = request.SizeId,
                    Quantity = request.Quantity
                });
                _logger.LogInformation("Added ItemId {ItemId} SizeId {SizeId} to cart with quantity {Quantity}.",
                    request.ItemId, request.SizeId, request.Quantity);
            }

            // Save to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Ok(new { Message = "Item added to cart successfully." });
        }

        /// <summary>
        /// Updates an existing item by ID.
        /// </summary>
        /// <param name="id">The ID of the item to update.</param>
        /// <param name="item">The updated item data (in request body).</param>
        /// <returns>200 OK if success, 400 or 404 if invalid.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] Item item)
        {
            if (id != item.ItemId)
            {
                return BadRequest(new { Error = "Item ID in path does not match item ID in body." });
            }

            var existingItem = await _context.Items
                .Include(i => i.ItemSizes)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (existingItem == null)
            {
                return NotFound(new { Error = "Item not found." });
            }

            existingItem.Name = item.Name;
            existingItem.CategoryId = item.CategoryId;
            existingItem.Price = item.Price;
            existingItem.Description = item.Description;
            existingItem.ImageUrl = item.ImageUrl;

            // If you need to handle changes to ItemSizes, do so here
            // existingItem.ItemSizes = item.ItemSizes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(id))
                    return NotFound(new { Error = "Item not found during concurrency check." });
                throw;
            }

            return Ok(new { Message = "Item updated successfully.", Item = existingItem });
        }

        /// <summary>
        /// Deletes an existing item by ID.
        /// </summary>
        /// <param name="id">The item ID to delete.</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound(new { Error = "Item not found." });
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item deleted successfully." });
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ItemId == id);
        }
    }

    /// <summary>
    /// Simple DTO for add-to-cart requests.
    /// </summary>
    public class AddToCartRequest
    {
        public int ItemId { get; set; }
        public int SizeId { get; set; }
        public int Quantity { get; set; }
    }
}