using OnlineShoppingSite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineShoppingSite.ViewModels;
using OnlineShoppingSite.Models.Requests;


namespace OnlineShoppingSite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        // GET: api/Admin/GetProduct/{id}
        [HttpGet("GetProduct/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Items.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        // GET: api/Admin/GetSize/{id}
        [HttpGet("GetSize/{id}")]
        public async Task<IActionResult> GetSize(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }
            return Ok(size);
        }

        // GET: api/Admin/GetCategory/{id}
        [HttpGet("GetCategory/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Category with ID {id} not found." });
            }
            return Ok(category);
        }

        // POST: api/Admin/CreateProduct
        [HttpPost("CreateProduct")]
        public async Task<IActionResult> CreateProduct([FromBody] ItemViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            _context.Items.Add(viewModel.Item);
            await _context.SaveChangesAsync();

            // Return HTTP 201 Created, pointing to the GetProduct action
            return CreatedAtAction(nameof(GetProduct), new { id = viewModel.Item.ItemId }, viewModel.Item);
        }

        // POST: api/Admin/CreateSize
        [HttpPost("CreateSize")]
        public async Task<IActionResult> CreateSize([FromBody] Size size)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Sizes.Add(size);
            await _context.SaveChangesAsync();

            // Return HTTP 201 Created, pointing to the GetSize action.
            return CreatedAtAction(nameof(GetSize), new { id = size.SizeId }, size);
        }

        // GET: api/Admin/GetProductForEdit/5
        [HttpGet("GetProductForEdit/{id}")]
        public async Task<IActionResult> GetProductForEdit(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Get a list of categories to populate a dropdown in React.
            var categories = await _context.Categories
                .Select(c => new { c.CategoryId, c.Name })
                .ToListAsync();

            var result = new 
            {
                item,
                categories
            };

            return Ok(result);
        }

        // GET: api/Admin/GetSizeForEdit/5
        [HttpGet("GetSizeForEdit/{id}")]
        public async Task<IActionResult> GetSizeForEdit(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }
            return Ok(size);
        }

        // GET: api/Admin/GetSizeAssignmentsForItem/5
        [HttpGet("GetSizeAssignmentsForItem/{id}")]
        public async Task<IActionResult> GetSizeAssignmentsForItem(int id)
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

            var result = new
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                SizeAssignments = allSizes.Select(s => new 
                {
                    SizeId = s.SizeId,
                    SizeName = s.Name,
                    Quantity = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == s.SizeId)?.Quantity ?? 0,
                    IsSelected = item.ItemSizes.Any(isz => isz.SizeId == s.SizeId)
                }).ToList()
            };

            return Ok(result);
        }


        // PUT: api/Admin/EditProduct/5
        [HttpPut("EditProduct/{id}")]
        public async Task<IActionResult> EditProduct(int id, [FromBody] ItemViewModel viewModel)
        {
            // Check if the id in the URL matches the id in the model
            if (id != viewModel.Item.ItemId)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            // Validate the incoming model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Update the product in the database
                _context.Update(viewModel.Item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // If the item doesn't exist, return a NotFound result
                if (!ItemExists(viewModel.Item.ItemId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(viewModel.Item);
        }

        // Helper method to check if an item exists
        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ItemId == id);
        }


        // PUT: api/Admin/EditSize/5
        [HttpPut("EditSize/{id}")]
        public async Task<IActionResult> EditSize(int id, [FromBody] Size size)
        {
            // Check if the ID from the URL matches the ID in the body.
            if (id != size.SizeId)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            // Validate the incoming model.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
                else
                {
                    throw;
                }
            }

            // Return the updated size object as JSON.
            return Ok(size);
        }

        // Helper method to check if a Size exists.
        private bool SizeExists(int id)
        {
            return _context.Sizes.Any(e => e.SizeId == id);
        }

        // POST: api/Admin/AssignSizesToItem
        [HttpPost("AssignSizesToItem")]
        public async Task<IActionResult> AssignSizesToItem([FromBody] ItemSizeViewModel model)
        {
            // Retrieve the item along with its current size assignments
            var item = await _context.Items
                                    .Include(i => i.ItemSizes)
                                    .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

            if (item == null)
            {
                return NotFound(new { message = $"Item with ID {model.ItemId} not found." });
            }

            // Determine which sizes are currently selected
            var selectedSizeIds = model.SizeAssignments
                                    .Where(sa => sa.IsSelected)
                                    .Select(sa => sa.SizeId)
                                    .ToList();

            // Remove any ItemSizes that are no longer selected
            var itemSizesToRemove = item.ItemSizes
                                        .Where(isz => !selectedSizeIds.Contains(isz.SizeId))
                                        .ToList();

            foreach (var itemSize in itemSizesToRemove)
            {
                item.ItemSizes.Remove(itemSize);
                _context.ItemSizes.Remove(itemSize);
            }

            // Add or update size assignments based on the incoming data
            foreach (var sizeAssignment in model.SizeAssignments)
            {
                var itemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == sizeAssignment.SizeId);

                if (sizeAssignment.IsSelected)
                {
                    if (itemSize == null)
                    {
                        // Add a new ItemSize record
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
                        // Update the quantity for an existing size assignment
                        itemSize.Quantity = sizeAssignment.Quantity;
                        _context.Entry(itemSize).State = EntityState.Modified;
                    }
                }
            }

            // Log each assignment for debugging purposes
            foreach (var assignment in model.SizeAssignments)
            {
                _logger.LogInformation("SizeId: {SizeId}, Quantity: {Quantity}, IsSelected: {IsSelected}",
                    assignment.SizeId, assignment.Quantity, assignment.IsSelected);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sizes assigned/updated for ItemId {ItemId}.", model.ItemId);

            // Return a JSON response indicating success
            return Ok(new { message = "Sizes assigned/updated successfully.", itemId = model.ItemId });
        }

        // DELETE: api/Admin/DeleteProduct/5
        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound(new { message = $"Product with id {id} not found." });
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product deleted successfully.", productId = id });
        }

        // DELETE: api/Admin/DeleteSize/5
        [HttpDelete("DeleteSize/{id}")]
        public async Task<IActionResult> DeleteSize(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound(new { message = $"Size with id {id} not found." });
            }
            
            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Size deleted successfully.", sizeId = id });
        }

        // GET: api/Admin/ViewOrder/5
        [HttpGet("ViewOrder/{id}")]
        public async Task<IActionResult> ViewOrder(int id)
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
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            return Ok(order);
        }

        // PUT: api/Admin/UpdateOrderStatus/5
        [HttpPut("UpdateOrderStatus/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            // Find the order by id
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            // Update the order's status
            order.Status = request.Status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Return a JSON response indicating success, along with the updated order
            return Ok(new { message = "Order status updated successfully.", order });
        }

        // POST: api/Admin/CreateCategory
        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Assuming you have a GetCategory endpoint to retrieve a category by its ID.
            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
        }

        // PUT: api/Admin/EditCategory/5
        [HttpPut("EditCategory/{id}")]
        public async Task<IActionResult> EditCategory(int id, [FromBody] Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest(new { message = "ID mismatch." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.CategoryId))
                {
                    return NotFound(new { message = $"Category with ID {category.CategoryId} not found." });
                }
                else
                {
                    throw;
                }
            }

            return Ok(category);
        }

        // Helper method to check if a category exists.
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(c => c.CategoryId == id);
        }

        // DELETE: api/Admin/DeleteCategory/5
        [HttpDelete("DeleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Category with ID {id} not found." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully.", categoryId = id });
        }
    }
}