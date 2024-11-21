using OnlineShoppingSite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineShoppingSite.ViewModels;


namespace OnlineShoppingSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/ManageProducts
        public async Task<IActionResult> ManageProducts()
        {
            var products = await _context.Items.ToListAsync();
            return View(products);
        }

        // GET: Admin/CreateProduct
        public IActionResult CreateProduct()
        {
            var categories = new SelectList(_context.Categories, "CategoryId", "Name");
            var viewModel = new ItemViewModel
            {
                Item = new Item(),
                Categories = categories
            };
            return View(viewModel);
        }

        // GET: Admin/ManageSizes
        public async Task<IActionResult> ManageSizes()
        {
            var sizes = await _context.Sizes.ToListAsync();
            return View(sizes);
        }

        // GET: Admin/CreateSize
        public IActionResult CreateSize()
        {
            return View();
        }

        // POST: Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ItemViewModel viewModel)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {
                _context.Items.Add(viewModel.Item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageProducts));
            }

            // Repopulate the categories in case of an error
            viewModel.Categories = new SelectList(_context.Categories, "CategoryId", "Name");
            return View(viewModel);
        }

        // POST: Admin/CreateSize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSize(Size size)
        {
            if (ModelState.IsValid)
            {
                _context.Sizes.Add(size);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageSizes));
            }
            return View(size);
        }

        // GET: Admin/EditProduct/5
        public async Task<IActionResult> EditProduct(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Create the ViewModel and populate it
            var viewModel = new ItemViewModel
            {
                Item = item,
                Categories = new SelectList(_context.Categories, "CategoryId", "Name", item.CategoryId)
            };

            return View(viewModel);
        }

        // GET: Admin/EditSize/5
        public async Task<IActionResult> EditSize(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }
            return View(size);
        }

        // GET: Admin/AssignSizesToItem/5
        public async Task<IActionResult> AssignSizesToItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            return View(viewModel);
        }

        // POST: Admin/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, ItemViewModel viewModel)
        {
            if (id != viewModel.Item.ItemId)
            {
                return NotFound();
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
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageProducts));
            }

            // Repopulate the categories in case of an error
            viewModel.Categories = new SelectList(_context.Categories, "CategoryId", "Name");
            return View(viewModel);
        }

        // POST: Admin/EditSize/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSize(int id, Size size)
        {
            if (id != size.SizeId)
            {
                return NotFound();
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
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageSizes));
            }
            return View(size);
        }

        // POST: Admin/AssignSizesToItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSizesToItem(ItemSizeViewModel model)
        {
            var item = await _context.Items
                                     .Include(i => i.ItemSizes)
                                     .FirstOrDefaultAsync(i => i.ItemId == model.ItemId);

            if (item == null)
            {
                return NotFound();
            }

            foreach (var sizeAssignment in model.SizeAssignments)
            {
                var itemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == sizeAssignment.SizeId);

                if (sizeAssignment.IsSelected)
                {
                    if (itemSize == null)
                    {
                        // Add new ItemSize
                        item.ItemSizes.Add(new ItemSize
                        {
                            SizeId = sizeAssignment.SizeId,
                            Quantity = sizeAssignment.Quantity
                        });
                    }
                    else
                    {
                        // Update existing quantity
                        itemSize.Quantity = sizeAssignment.Quantity;
                    }
                }
                else
                {
                    if (itemSize != null)
                    {
                        // Remove ItemSize
                        _context.ItemSizes.Remove(itemSize);
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sizes assigned/updated for ItemId {ItemId}.", model.ItemId);

            return RedirectToAction(nameof(ManageProducts));
        }

        // GET: Admin/DeleteProduct/5
        public async Task<IActionResult> DeleteProduct(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Admin/DeleteSize/5
        public async Task<IActionResult> DeleteSize(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var size = await _context.Sizes
                .FirstOrDefaultAsync(m => m.SizeId == id);
            if (size == null)
            {
                return NotFound();
            }

            return View(size);
        }

        // POST: Admin/DeleteProduct/5
        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageProducts));
        }

        // POST: Admin/DeleteSize/5
        [HttpPost, ActionName("DeleteSize")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSizeConfirmed(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageSizes));
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ItemId == id);
        }

        private bool SizeExists(int id)
        {
            return _context.Sizes.Any(e => e.SizeId == id);
        }

        // GET: Admin/BulkUploadProducts
        public IActionResult BulkUploadProducts()
        {
            return View();
        }

        // POST: Admin/BulkUploadProducts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUploadProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a CSV file.");
                return View();
            }

            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    string headerLine = await stream.ReadLineAsync(); // Read header line
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
                            // Map other fields as necessary
                        };

                        _context.Items.Add(item);
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageProducts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk upload.");
                ModelState.AddModelError("", "An error occurred during bulk upload.");
                return View();
            }
        }

        // GET: Admin/ManageOrders
        public async Task<IActionResult> ManageOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .ToListAsync();
            return View(orders);
        }

        // GET: Admin/ViewOrder/5
        public async Task<IActionResult> ViewOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            return View(order);
        }

        // POST: Admin/UpdateOrderStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewOrder), new { id });
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: Admin/ManageCategories
        public async Task<IActionResult> ManageCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        // GET: Admin/CreateCategory
        public IActionResult CreateCategory()
        {
            return View();
        }

        // POST: Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageCategories));
            }
            return View(category);
        }

        // GET: Admin/EditCategory/5
        public async Task<IActionResult> EditCategory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/EditCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
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
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageCategories));
            }
            return View(category);
        }

        // GET: Admin/DeleteCategory/5
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/DeleteCategory/5
        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageCategories));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }

    }
}
