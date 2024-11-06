// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.Extensions;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;
        private readonly IConfiguration _configuration;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Retrieve cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            _logger.LogInformation("Cart Index action called. Cart has {ItemCount} items.", cart.Count);
            return View(cart);
        }

        // GET: Cart/Checkout
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (!cart.Any())
            {
                _logger.LogWarning("Checkout attempted with empty cart.");
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Extract all ItemIds from the cart
            var itemIds = cart.Select(c => c.Item.ItemId).ToList();

            // Fetch all corresponding Items from the database
            var items = _context.Items.Where(i => itemIds.Contains(i.ItemId)).ToList();

            // Check if any Items are missing
            var missingItemIds = itemIds.Except(items.Select(i => i.ItemId)).ToList();
            if (missingItemIds.Any())
            {
                _logger.LogWarning("Checkout GET action: Items with IDs {MissingItemIds} not found.", string.Join(", ", missingItemIds));
                TempData["Error"] = "Some items in your cart are no longer available.";
                return RedirectToAction("Index");
            }

            // Create the Order object with fully populated OrderItems
            var order = new Order
            {
                ShippingDetails = new ShippingDetails(),
                OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.Item.ItemId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Item.Price,
                    Item = items.FirstOrDefault(i => i.ItemId == c.Item.ItemId) // Assign the Item
                }).ToList(),
                TotalAmount = cart.Sum(c => c.Item.Price * c.Quantity)
            };

            _logger.LogInformation("Checkout GET action called. Order total amount: {TotalAmount}", order.TotalAmount);

            return View(order);
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order, string stripeToken)
        {
            _logger.LogInformation("Checkout POST action called.");

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            _logger.LogInformation("Cart retrieved from session. Cart has {ItemCount} items.", cart.Count);

            if (!cart.Any())
            {
                ModelState.AddModelError("", "Your cart is empty.");
                _logger.LogWarning("Checkout POST action: cart is empty.");
                return View(order);
            }

            // Remove OrderItems and ChargeId from model binding since they come from the cart and processing
            ModelState.Remove(nameof(Order.OrderItems));
            ModelState.Remove(nameof(Order.ChargeId));

            // Extract all ItemIds from the cart
            var itemIds = cart.Select(c => c.Item.ItemId).ToList();

            // Fetch all corresponding Items from the database
            var items = await _context.Items.Where(i => itemIds.Contains(i.ItemId)).ToListAsync();

            // Check if any Items are missing
            var missingItemIds = itemIds.Except(items.Select(i => i.ItemId)).ToList();
            if (missingItemIds.Any())
            {
                ModelState.AddModelError("", "Some items in your cart are no longer available.");
                _logger.LogWarning("Checkout POST action: Items with IDs {MissingItemIds} not found.", string.Join(", ", missingItemIds));

                // Reconstruct OrderItems with available items
                order.OrderItems = cart
                    .Where(c => items.Any(i => i.ItemId == c.Item.ItemId))
                    .Select(c => new OrderItem
                    {
                        ItemId = c.Item.ItemId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Item.Price,
                        Item = items.FirstOrDefault(i => i.ItemId == c.Item.ItemId)
                    }).ToList();
                order.TotalAmount = order.OrderItems.Sum(c => c.UnitPrice * c.Quantity);

                _logger.LogInformation("After removing missing items, OrderTotalAmount: {TotalAmount}", order.TotalAmount);

                return View(order);
            }

            // Calculate TotalAmount and log each item's details
            decimal calculatedTotal = 0.0m;
            foreach (var c in cart)
            {
                _logger.LogInformation("Cart Item - ID: {ItemId}, Name: {ItemName}, Price: {UnitPrice}, Quantity: {Quantity}",
                    c.Item.ItemId,
                    c.Item.Name,
                    c.Item.Price,
                    c.Quantity);
                calculatedTotal += c.Item.Price * c.Quantity;
            }
            _logger.LogInformation("Calculated Total Amount: {CalculatedTotal}", calculatedTotal);

            // Validate that TotalAmount is at least $0.01
            if (calculatedTotal < 0.01m)
            {
                ModelState.AddModelError("", "Order total must be at least $0.01.");
                _logger.LogWarning("Checkout POST action: Order total {TotalAmount} is invalid.", calculatedTotal);

                // Reconstruct OrderItems with Items
                order.OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.Item.ItemId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Item.Price,
                    Item = items.FirstOrDefault(i => i.ItemId == c.Item.ItemId)
                }).ToList();
                order.TotalAmount = calculatedTotal;

                return View(order);
            }

            // Assign calculated total to order
            order.TotalAmount = calculatedTotal;

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Proceeding to process payment and save order.");

                // Set PaymentMethod programmatically or via form
                // e.g., order.PaymentMethod = "Credit Card";

                // Log TotalAmount before Stripe charge
                _logger.LogInformation("Order Total Amount before Stripe charge: {TotalAmount}", order.TotalAmount);

                // Process payment with Stripe
                var chargeId = await CreateStripeCharge(order.TotalAmount, stripeToken);

                if (string.IsNullOrEmpty(chargeId))
                {
                    ModelState.AddModelError("", "Payment processing failed. Please try again.");
                    _logger.LogError("Stripe charge failed for Order.");

                    // Reconstruct OrderItems with Items
                    order.OrderItems = cart.Select(c => new OrderItem
                    {
                        ItemId = c.Item.ItemId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Item.Price,
                        Item = items.FirstOrDefault(i => i.ItemId == c.Item.ItemId)
                    }).ToList();
                    order.TotalAmount = calculatedTotal;

                    return View(order);
                }

                _logger.LogInformation("Payment processed successfully with Charge ID: {ChargeId}", chargeId);

                // Save shipping details
                _context.ShippingDetails.Add(order.ShippingDetails);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Shipping details saved with ID: {ShippingDetailsId}", order.ShippingDetails.ShippingDetailsId);

                // Assign the Item properties to OrderItems
                order.OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.Item.ItemId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Item.Price,
                    Item = items.FirstOrDefault(i => i.ItemId == c.Item.ItemId)
                }).ToList();
                order.ChargeId = chargeId; // Set ChargeId

                // Create and save the order
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order saved with ID: {OrderId}", order.OrderId);

                // Clear the cart
                HttpContext.Session.Remove("Cart");
                _logger.LogInformation("Cart cleared from session.");

                return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
            }
            else
            {
                // Log model state errors
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join("; ", errors));
            }

            // Recalculate total and items in case of validation errors
            var itemsInCart = await _context.Items.Where(i => itemIds.Contains(i.ItemId)).ToListAsync();

            order.OrderItems = cart
                .Where(c => itemsInCart.Any(i => i.ItemId == c.Item.ItemId))
                .Select(c => new OrderItem
                {
                    ItemId = c.Item.ItemId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Item.Price,
                    Item = itemsInCart.FirstOrDefault(i => i.ItemId == c.Item.ItemId)
                }).ToList();
            order.TotalAmount = order.OrderItems.Sum(c => c.UnitPrice * c.Quantity);

            _logger.LogInformation("After ModelState invalid, OrderTotalAmount: {TotalAmount}", order.TotalAmount);

            return View(order);
        }

        // GET: Cart/OrderConfirmation
        public IActionResult OrderConfirmation(int id)
        {
            _logger.LogInformation("OrderConfirmation action called with OrderId: {OrderId}", id);

            var order = _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                _logger.LogWarning("OrderConfirmation: Order not found with ID: {OrderId}", id);
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.Item.ItemId == id);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                _logger.LogInformation("Item with ID {ItemId} removed from cart.", id);
            }
            else
            {
                _logger.LogWarning("Attempted to remove item with ID {ItemId} which was not found in cart.", id);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Creates a charge using Stripe API.
        /// </summary>
        /// <param name="amount">Amount in USD</param>
        /// <param name="stripeToken">Stripe Token from client</param>
        /// <returns>Charge ID if successful; otherwise, null</returns>
        private async Task<string> CreateStripeCharge(decimal amount, string stripeToken)
        {
            try
            {
                // Validate amount
                if (amount < 0.01m)
                {
                    _logger.LogError("Stripe Error: Amount {Amount} is less than the minimum required.", amount);
                    return null;
                }

                var options = new ChargeCreateOptions
                {
                    Amount = (long)(amount * 100), // Stripe expects amount in cents
                    Currency = "usd",
                    Description = "Order Charge",
                    Source = stripeToken,
                };

                var service = new ChargeService();
                Charge charge = await service.CreateAsync(options);

                return charge.Id;
            }
            catch (StripeException ex)
            {
                _logger.LogError("Stripe Error: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected Error: {Message}", ex.Message);
                return null;
            }
        }
    }
}