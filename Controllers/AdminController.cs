using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // -----------------------
        // PRODUCTS ENDPOINTS
        // -----------------------
        
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Items.ToListAsync();
            return Ok(products);
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound("Product not found.");
            return Ok(item);
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] Item product)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Items.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProductById), new { id = product.ItemId }, product);
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Item product)
        {
            if (id != product.ItemId) return BadRequest("Product ID mismatch.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Entry(product).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                return Ok(product);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(id)) return NotFound("Product not found.");
                throw;
            }
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound("Product not found.");

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // -----------------------
        // CATEGORIES ENDPOINTS
        // -----------------------
        
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        [HttpGet("categories/{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound("Category not found.");
            return Ok(category);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryId }, category);
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            if (id != category.CategoryId) return BadRequest("Category ID mismatch.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Entry(category).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                return Ok(category);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id)) return NotFound("Category not found.");
                throw;
            }
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound("Category not found.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // -----------------------
        // SIZES ENDPOINTS
        // -----------------------
        
        [HttpGet("sizes")]
        public async Task<IActionResult> GetSizes()
        {
            var sizes = await _context.Sizes.ToListAsync();
            return Ok(sizes);
        }

        [HttpGet("sizes/{id}")]
        public async Task<IActionResult> GetSizeById(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return NotFound("Size not found.");
            return Ok(size);
        }

        [HttpPost("sizes")]
        public async Task<IActionResult> CreateSize([FromBody] Size size)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Sizes.Add(size);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSizeById), new { id = size.SizeId }, size);
        }

        [HttpPut("sizes/{id}")]
        public async Task<IActionResult> UpdateSize(int id, [FromBody] Size size)
        {
            if (id != size.SizeId) return BadRequest("Size ID mismatch.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Entry(size).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                return Ok(size);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SizeExists(id)) return NotFound("Size not found.");
                throw;
            }
        }

        [HttpDelete("sizes/{id}")]
        public async Task<IActionResult> DeleteSize(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return NotFound("Size not found.");

            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // -----------------------
        // ASSIGN SIZES TO ITEM
        // -----------------------
        
        [HttpGet("items/{id}/sizes")]
        public async Task<IActionResult> GetItemSizes(int id)
        {
            var item = await _context.Items
                .Include(i => i.ItemSizes)
                .ThenInclude(isz => isz.Size)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null) return NotFound("Item not found.");

            var assignedSizes = item.ItemSizes.Select(isz => new
            {
                SizeId = isz.SizeId,
                isz.Size.Name,
                isz.Quantity
            }).ToList();

            return Ok(new
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                AssignedSizes = assignedSizes
            });
        }

        [HttpPost("items/{id}/assign-sizes")]
        public async Task<IActionResult> AssignSizesToItem(int id, [FromBody] ItemSizeViewModel model)
        {
            var item = await _context.Items
                .Include(i => i.ItemSizes)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null) return NotFound("Item not found.");

            // Remove sizes not selected anymore
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

            // Add or Update selected sizes
            foreach (var sizeAssignment in model.SizeAssignments)
            {
                if (sizeAssignment.IsSelected)
                {
                    var existingItemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == sizeAssignment.SizeId);
                    if (existingItemSize == null)
                    {
                        var newItemSize = new ItemSize
                        {
                            ItemId = item.ItemId,
                            SizeId = sizeAssignment.SizeId,
                            Quantity = sizeAssignment.Quantity
                        };
                        item.ItemSizes.Add(newItemSize);
                        _context.ItemSizes.Add(newItemSize);
                    }
                    else
                    {
                        existingItemSize.Quantity = sizeAssignment.Quantity;
                        _context.Entry(existingItemSize).State = EntityState.Modified;
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sizes assigned/updated for ItemId {ItemId}.", id);

            return Ok("Sizes assigned successfully.");
        }


        // -----------------------
        // ORDERS ENDPOINTS
        // -----------------------

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .ToListAsync();
            return Ok(orders);
        }

        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound("Order not found.");

            return Ok(order);
        }

        [HttpPost("orders/{id}/update-status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateModel model)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found.");

            order.Status = model.Status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order status updated successfully." });
        }


        // -----------------------
        // BULK UPLOAD PRODUCTS
        // -----------------------
        
        [HttpPost("products/bulk-upload")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> BulkUploadProducts()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please select a CSV file.");
            }

            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    string headerLine = await stream.ReadLineAsync(); // read header line
                    while (!stream.EndOfStream)
                    {
                        string line = await stream.ReadLineAsync();
                        var values = line.Split(',');

                        if (values.Length < 4)
                        {
                            _logger.LogWarning("Skipping line due to insufficient columns: {Line}", line);
                            continue;
                        }

                        if (!decimal.TryParse(values[1], out decimal price))
                        {
                            _logger.LogWarning("Invalid price: {PriceValue}", values[1]);
                            continue;
                        }

                        var item = new Item
                        {
                            Name = values[0],
                            Price = price,
                            Description = values[2],
                            ImageUrl = values[3]
                            // Map other fields if necessary
                        };

                        _context.Items.Add(item);
                    }
                }
                await _context.SaveChangesAsync();
                return Ok("Bulk upload completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk upload.");
                return StatusCode(500, "An error occurred during bulk upload.");
            }
        }


        // -----------------------
        // DASHBOARD (Optional)
        // -----------------------
        
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            // If you have some summary stats to return
            // For now, just return a placeholder
            return Ok(new { message = "Dashboard data here (if needed)." });
        }


        // -----------------------
        // HELPER METHODS
        // -----------------------

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
    }

    // Simple DTO for order status updates
    public class OrderStatusUpdateModel
    {
        public string Status { get; set; }
    }
}
