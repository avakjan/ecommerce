using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using OnlineShoppingSite.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OnlineShoppingSite.Controllers.Api
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

        [HttpGet]
        public async Task<IActionResult> GetItems([FromQuery] int categoryId = 0)
        {
            var categories = await GetCategoriesFromCacheAsync();
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
            return Ok(new ItemsViewModel
            {
                Items = items,
                Categories = categories,
                SelectedCategoryId = categoryId
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemSizes)
                    .ThenInclude(isz => isz.Size)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (item == null)
            {
                _logger.LogWarning("GetItem: Item not found with ID {ItemId}.", id);
                return NotFound(new { error = "Item not found" });
            }

            var viewModel = new ItemDetailsViewModel
            {
                Item = item,
                SizeId = null,
                Quantity = 1
            };

            return Ok(viewModel);
        }

        [HttpPost("addtocart")]
        public IActionResult AddToCart([FromBody] AddToCartModel model)
        {
            if (model.Quantity < 1)
            {
                return BadRequest(new { error = "Quantity must be at least 1." });
            }

            var itemSize = _context.ItemSizes
                .FirstOrDefault(isz => isz.ItemId == model.ItemId && isz.SizeId == model.SizeId);

            if (itemSize == null)
            {
                return BadRequest(new { error = "Selected size is not available for this item." });
            }

            if (itemSize.Quantity < model.Quantity)
            {
                return BadRequest(new { error = "This size is out of stock." });
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.ItemId == model.ItemId && c.SizeId == model.SizeId);

            if (cartItem != null)
            {
                cartItem.Quantity += model.Quantity;
                _logger.LogInformation("Increased quantity of ItemId {ItemId} SizeId {SizeId} in cart by {Quantity}.",
                    model.ItemId, model.SizeId, model.Quantity);
            }
            else
            {
                cart.Add(new CartItem { ItemId = model.ItemId, SizeId = model.SizeId, Quantity = model.Quantity });
                _logger.LogInformation("Added ItemId {ItemId} SizeId {SizeId} to cart with quantity {Quantity}.",
                    model.ItemId, model.SizeId, model.Quantity);
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return Ok(new {
                message = "Item added to cart successfully",
                cart = cart
            });
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedProducts()
        {
            try
            {
                var featuredItems = await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.ItemSizes)
                        .ThenInclude(isz => isz.Size)
                    .Where(i => i.IsFeatured)
                    .Take(6)
                    .ToListAsync();

                return Ok(featuredItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured products");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            try
            {
                var searchResults = await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.ItemSizes)
                        .ThenInclude(isz => isz.Size)
                    .Where(i => i.Name.Contains(query) ||
                               i.Description.Contains(query) ||
                               i.Category.Name.Contains(query))
                    .ToListAsync();

                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with query: {Query}", query);
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<List<Category>> GetCategoriesFromCacheAsync()
        {
            if (!_cache.TryGetValue("Categories", out List<Category> categories))
            {
                categories = await _context.Categories.ToListAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(60));
                _cache.Set("Categories", categories, cacheEntryOptions);
            }
            return categories;
        }
    }
}