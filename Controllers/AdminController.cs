using OnlineShoppingSite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
            return View();
        }

        // POST: Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Item item)
        {
            if (ModelState.IsValid)
            {
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageProducts));
            }
            return View(item);
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
            return View(item);
        }

        // POST: Admin/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Item item)
        {
            if (id != item.ItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.ItemId))
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
            return View(item);
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

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ItemId == id);
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

    }
}
