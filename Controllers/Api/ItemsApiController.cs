using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OnlineShoppingSite.Extensions;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace OnlineShoppingSite.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ItemsApiController> _logger;
        private readonly IMemoryCache _cache;

        public ItemsApiController(ApplicationDbContext context, ILogger<ItemsApiController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        // GET: api/items
        [HttpGet]
        public async Task<ActionResult<ItemsViewModel>> GetItems([FromQuery] int categoryId = 0)
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

            return Ok(new ItemsViewModel
            {
                Items = items,
                Categories = categories,
                SelectedCategoryId = categoryId
            });
        }

        // GET: api/items/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDetailsViewModel>> GetItem(int id)
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

        // POST: api/items/addtocart
        [HttpPost("addtocart")]
        public ActionResult AddToCart([FromBody] AddToCartModel model)
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
                cart.Add(new CartItem 
                { 
                    ItemId = model.ItemId, 
                    SizeId = model.SizeId, 
                    Quantity = model.Quantity 
                });
                _logger.LogInformation("Added ItemId {ItemId} SizeId {SizeId} to cart with quantity {Quantity}.", 
                    model.ItemId, model.SizeId, model.Quantity);
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return Ok(new { 
                message = "Item added to cart successfully",
                cart = cart
            });
        }
    }

    public class ItemsViewModel
    {
        public List<Item> Items { get; set; }
        public List<Category> Categories { get; set; }
        public int SelectedCategoryId { get; set; }
    }

    public class AddToCartModel
    {
        public int ItemId { get; set; }
        public int SizeId { get; set; }
        public int Quantity { get; set; }
    }
}
