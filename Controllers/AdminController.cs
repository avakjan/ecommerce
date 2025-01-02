using OnlineShoppingSite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using OnlineShoppingSite.ViewModels;

namespace OnlineShoppingSite.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Admin/products
        // This replaces ManageProducts()
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Items.ToListAsync();
            return Ok(products); // Returns JSON
        }

        // POST: api/Admin/products
        // This replaces CreateProduct (POST)
        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] ItemViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Items.Add(viewModel.Item);
            await _context.SaveChangesAsync();

            // 201 Created with the new item’s info
            return CreatedAtAction(nameof(GetProductById),
                new { id = viewModel.Item.ItemId },
                viewModel.Item);
        }

        // GET: api/Admin/products/{id}
        // A helper endpoint to fetch a single product by ID
        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound(new { Error = "Product not found" });

            return Ok(item);
        }

        // PUT: api/Admin/products/{id}
        // This replaces EditProduct (POST)
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ItemViewModel viewModel)
        {
            if (id != viewModel.Item.ItemId)
                return BadRequest(new { Error = "ID mismatch" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _context.Update(viewModel.Item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(viewModel.Item.ItemId))
                    return NotFound(new { Error = "Product does not exist" });
                throw;
            }

            return Ok(new { Message = "Product updated successfully" });
        }

        // DELETE: api/Admin/products/{id}
        // This replaces DeleteProduct (POST)
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound(new { Error = "Product not found" });

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product deleted successfully" });
        }

        // GET: api/Admin/sizes
        // This replaces ManageSizes()
        [HttpGet("sizes")]
        public async Task<IActionResult> GetSizes()
        {
            var sizes = await _context.Sizes.ToListAsync();
            return Ok(sizes);
        }

        // POST: api/Admin/sizes
        // This replaces CreateSize (POST)
        [HttpPost("sizes")]
        public async Task<IActionResult> CreateSize([FromBody] Size size)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Sizes.Add(size);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSizeById), 
                new { id = size.SizeId }, 
                size);
        }

        // GET: api/Admin/sizes/{id}
        // Helper to get a single size
        [HttpGet("sizes/{id}")]
        public async Task<IActionResult> GetSizeById(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
                return NotFound(new { Error = "Size not found" });

            return Ok(size);
        }

        // PUT: api/Admin/sizes/{id}
        // This replaces EditSize (POST)
        [HttpPut("sizes/{id}")]
        public async Task<IActionResult> UpdateSize(int id, [FromBody] Size size)
        {
            if (id != size.SizeId)
                return BadRequest(new { Error = "ID mismatch" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _context.Update(size);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SizeExists(size.SizeId))
                    return NotFound(new { Error = "Size does not exist" });
                throw;
            }

            return Ok(new { Message = "Size updated successfully" });
        }

        // DELETE: api/Admin/sizes/{id}
        // This replaces DeleteSize (POST)
        [HttpDelete("sizes/{id}")]
        public async Task<IActionResult> DeleteSize(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
                return NotFound(new { Error = "Size not found" });

            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Size deleted successfully" });
        }

        // POST: api/Admin/products/{itemId}/assignSizes
        // This replaces AssignSizesToItem (POST)
        [HttpPost("products/{itemId}/assignSizes")]
        public async Task<IActionResult> AssignSizesToItem(int itemId, [FromBody] ItemSizeViewModel model)
        {
            if (itemId != model.ItemId)
                return BadRequest(new { Error = "ID mismatch" });

            var item = await _context.Items
                .Include(i => i.ItemSizes)
                .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

            if (item == null)
                return NotFound(new { Error = "Item not found" });

            // Remove ItemSizes that are no longer selected
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

            // Add/update selected sizes
            foreach (var assignment in model.SizeAssignments)
            {
                var itemSize = item.ItemSizes
                    .FirstOrDefault(isz => isz.SizeId == assignment.SizeId);

                if (assignment.IsSelected)
                {
                    if (itemSize == null)
                    {
                        // Add new ItemSize
                        var newItemSize = new ItemSize
                        {
                            ItemId = item.ItemId,
                            SizeId = assignment.SizeId,
                            Quantity = assignment.Quantity
                        };
                        item.ItemSizes.Add(newItemSize);
                        _context.ItemSizes.Add(newItemSize);
                    }
                    else
                    {
                        // Update existing quantity
                        itemSize.Quantity = assignment.Quantity;
                        _context.Entry(itemSize).State = EntityState.Modified;
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sizes assigned/updated for ItemId {ItemId}.", model.ItemId);

            return Ok(new { Message = "Sizes successfully assigned/updated." });
        }

        // GET: api/Admin/orders
        // This replaces ManageOrders()
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .ToListAsync();
            return Ok(orders);
        }

        // GET: api/Admin/orders/{id}
        // This replaces ViewOrder()
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound(new { Error = "Order not found" });

            return Ok(order);
        }

        // POST: api/Admin/orders/{id}/updateStatus
        // This replaces UpdateOrderStatus()
        [HttpPost("orders/{id}/updateStatus")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { Error = "Order not found" });

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Order status updated to {status}" });
        }

        // POST: api/Admin/products/bulkUpload
        // This replaces BulkUploadProducts() (POST)
        // Example CSV format:
        // Name,Price,Description,ImageUrl
        [HttpPost("products/bulkUpload")]
        public async Task<IActionResult> BulkUploadProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Error = "Please upload a valid CSV file." });

            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                string headerLine = await stream.ReadLineAsync(); // skip the header line if any

                while (!stream.EndOfStream)
                {
                    string? line = await stream.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    var values = line.Split(',');

                    // You may wish to handle invalid rows more gracefully
                    var item = new Item
                    {
                        Name = values[0],
                        Price = decimal.Parse(values[1]),
                        Description = values[2],
                        ImageUrl = values[3]
                        // Map other fields as necessary
                    };
                    _context.Items.Add(item);
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Bulk upload completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk upload.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "An error occurred during bulk upload." });
            }
        }

        // GET: api/Admin/categories
        // This replaces ManageCategories()
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        // POST: api/Admin/categories
        // This replaces CreateCategory (POST)
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategoryById),
                new { id = category.CategoryId },
                category);
        }

        // GET: api/Admin/categories/{id}
        [HttpGet("categories/{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { Error = "Category not found" });

            return Ok(category);
        }

        // PUT: api/Admin/categories/{id}
        // This replaces EditCategory (POST)
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            if (id != category.CategoryId)
                return BadRequest(new { Error = "ID mismatch" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.CategoryId))
                    return NotFound(new { Error = "Category does not exist" });
                throw;
            }

            return Ok(new { Message = "Category updated successfully" });
        }

        // DELETE: api/Admin/categories/{id}
        // This replaces DeleteCategory (POST)
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { Error = "Category not found" });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Category deleted successfully" });
        }

        #region Private Helpers

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