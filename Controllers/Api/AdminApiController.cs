using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;

namespace OnlineShoppingSite.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminApiController> _logger;

        public AdminApiController(ApplicationDbContext context, ILogger<AdminApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Products Management
        // GET: api/admin/products
        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<Item>>> GetProducts()
        {
            var products = await _context.Items.ToListAsync();
            return Ok(products);
        }

        // POST: api/admin/products
        [HttpPost("products")]
        public async Task<ActionResult<Item>> CreateProduct(ItemViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                _context.Items.Add(viewModel.Item);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetProduct), new { id = viewModel.Item.ItemId }, viewModel.Item);
            }
            return BadRequest(ModelState);
        }

        // GET: api/admin/products/{id}
        [HttpGet("products/{id}")]
        public async Task<ActionResult<Item>> GetProduct(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        // PUT: api/admin/products/{id}
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ItemViewModel viewModel)
        {
            if (id != viewModel.Item.ItemId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(viewModel.Item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(viewModel.Item.ItemId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return NoContent();
            }
            return BadRequest(ModelState);
        }

        // DELETE: api/admin/products/{id}
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        #endregion

        #region Sizes Management
        // GET: api/admin/sizes
        [HttpGet("sizes")]
        public async Task<ActionResult<IEnumerable<Size>>> GetSizes()
        {
            var sizes = await _context.Sizes.ToListAsync();
            return Ok(sizes);
        }

        // POST: api/admin/sizes
        [HttpPost("sizes")]
        public async Task<ActionResult<Size>> CreateSize(Size size)
        {
            if (ModelState.IsValid)
            {
                _context.Sizes.Add(size);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetSize), new { id = size.SizeId }, size);
            }
            return BadRequest(ModelState);
        }

        // GET: api/admin/sizes/{id}
        [HttpGet("sizes/{id}")]
        public async Task<ActionResult<Size>> GetSize(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }
            return Ok(size);
        }

        // PUT: api/admin/sizes/{id}
        [HttpPut("sizes/{id}")]
        public async Task<IActionResult> UpdateSize(int id, Size size)
        {
            if (id != size.SizeId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(size);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SizeExists(size.SizeId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return NoContent();
            }
            return BadRequest(ModelState);
        }

        // DELETE: api/admin/sizes/{id}
        [HttpDelete("sizes/{id}")]
        public async Task<IActionResult> DeleteSize(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }

            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        #endregion

        #region Categories Management
        // GET: api/admin/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        // POST: api/admin/categories
        [HttpPost("categories")]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
            }
            return BadRequest(ModelState);
        }

        // GET: api/admin/categories/{id}
        [HttpGet("categories/{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        // PUT: api/admin/categories/{id}
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return NoContent();
            }
            return BadRequest(ModelState);
        }

        // DELETE: api/admin/categories/{id}
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        #endregion

        #region Orders Management
        // GET: api/admin/orders
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .ToListAsync();
            return Ok(orders);
        }

        // GET: api/admin/orders/{id}
        [HttpGet("orders/{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        // PUT: api/admin/orders/{id}/status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region Item Sizes Management
        // GET: api/admin/products/{id}/sizes
        [HttpGet("products/{id}/sizes")]
        public async Task<ActionResult<ItemSizeViewModel>> GetItemSizes(int id)
        {
            var item = await _context.Items
                .Include(i => i.ItemSizes)
                .ThenInclude(isz => isz.Size)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            var allSizes = await _context.Sizes.ToListAsync();

            var viewModel = new ItemSizeViewModel
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                SizeAssignments = allSizes.Select(s => new ItemSizeViewModel.ItemSizeAssignment
                {
                    SizeId = s.SizeId,
                    SizeName = s.Name,
                    Quantity = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == s.SizeId)?.Quantity ?? 0,
                    IsSelected = item.ItemSizes.Any(isz => isz.SizeId == s.SizeId)
                }).ToList()
            };

            return Ok(viewModel);
        }

        // POST: api/admin/products/{id}/sizes
        [HttpPost("products/{id}/sizes")]
        public async Task<IActionResult> AssignSizesToItem(int id, ItemSizeViewModel model)
        {
            if (id != model.ItemId)
            {
                return BadRequest();
            }

            var item = await _context.Items
                .Include(i => i.ItemSizes)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            var selectedSizeIds = model.SizeAssignments
                .Where(sa => sa.IsSelected)
                .Select(sa => sa.SizeId)
                .ToList();

            var itemSizesToRemove = item.ItemSizes
                .Where(isz => !selectedSizeIds.Contains(isz.SizeId))
                .ToList();

            foreach (var itemSize in itemSizesToRemove)
            {
                item.ItemSizes.Remove(itemSize);
                _context.ItemSizes.Remove(itemSize);
            }

            foreach (var sizeAssignment in model.SizeAssignments.Where(sa => sa.IsSelected))
            {
                var itemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == sizeAssignment.SizeId);

                if (itemSize == null)
                {
                    var newItemSize = new ItemSize
                    {
                        ItemId = item.ItemId,
                        SizeId = sizeAssignment.SizeId,
                        Quantity = sizeAssignment.Quantity
                    };
                    item.ItemSizes.Add(newItemSize);
                }
                else
                {
                    itemSize.Quantity = sizeAssignment.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        #endregion

        #region Bulk Upload
        // POST: api/admin/products/bulk
        [HttpPost("products/bulk")]
        public async Task<IActionResult> BulkUploadProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please select a CSV file.");
            }

            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    string headerLine = await stream.ReadLineAsync();
                    while (!stream.EndOfStream)
                    {
                        string line = await stream.ReadLineAsync();
                        var values = line.Split(',');

                        var item = new Item
                        {
                            Name = values[0],
                            Price = decimal.Parse(values[1]),
                            Description = values[2],
                            ImageUrl = values[3]
                        };

                        _context.Items.Add(item);
                    }
                }
                await _context.SaveChangesAsync();
                return Ok(new { message = "Bulk upload completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk upload.");
                return BadRequest(new { error = "An error occurred during bulk upload." });
            }
        }
        #endregion

        #region Helper Methods
        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ItemId == id);
        }

        private bool SizeExists(int id)
        {
            return _context.Sizes.Any(e => e.SizeId == id);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
        #endregion
    }
}
