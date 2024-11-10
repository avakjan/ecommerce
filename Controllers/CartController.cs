// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.Extensions;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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

            // Initialize Stripe with the Secret Key from configuration
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        /// <summary>
        /// Displays the current items in the shopping cart.
        /// </summary>
        /// <returns>View with CartViewModel containing cart items and their details.</returns>
        // GET: Cart/Index
        public IActionResult Index()
        {
            // Retrieve cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            _logger.LogInformation("Cart Index action called. Cart has {ItemCount} items.", cart.Count);

            if (cart.Any())
            {
                // Fetch all items from the database
                var itemIds = cart.Select(c => c.ItemId).ToList();
                var items = _context.Items.Where(i => itemIds.Contains(i.ItemId)).ToList();

                // Create and populate the ViewModel
                var viewModel = new CartViewModel
                {
                    CartItems = cart,
                    Items = items
                };

                return View(viewModel);
            }
            else
            {
                // Return an empty ViewModel if cart is empty
                var emptyViewModel = new CartViewModel
                {
                    CartItems = new List<CartItem>(),
                    Items = new List<Item>()
                };
                return View(emptyViewModel);
            }
        }

        /// <summary>
        /// Displays the checkout page with order details and payment form.
        /// </summary>
        /// <returns>Checkout view with Order model and Payment Intent client secret.</returns>
        // GET: Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            // Retrieve cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (!cart.Any())
            {
                _logger.LogWarning("Checkout attempted with empty cart.");
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Extract all ItemIds from the cart
            var itemIds = cart.Select(c => c.ItemId).ToList();

            // Fetch all corresponding Items from the database
            var items = await _context.Items.Where(i => itemIds.Contains(i.ItemId)).ToListAsync();

            // Check if any Items are missing
            var missingItemIds = itemIds.Except(items.Select(i => i.ItemId)).ToList();
            if (missingItemIds.Any())
            {
                _logger.LogWarning("Checkout GET action: Items with IDs {MissingItemIds} not found.", string.Join(", ", missingItemIds));
                TempData["Error"] = "Some items in your cart are no longer available.";
                return RedirectToAction("Index");
            }

            // Calculate TotalAmount
            decimal totalAmount = cart.Sum(c => (items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0) * c.Quantity);

            // Create Payment Intent
            var paymentIntentClientSecret = await CreateStripePaymentIntentAsync(totalAmount);

            if (string.IsNullOrEmpty(paymentIntentClientSecret))
            {
                TempData["Error"] = "Unable to process payment at this time. Please try again later.";
                return RedirectToAction("Index");
            }

            // Create the Order object with OrderItems
            var order = new Order
            {
                ShippingDetails = new ShippingDetails(),
                OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    Item = items.FirstOrDefault(i => i.ItemId == c.ItemId),
                    Quantity = c.Quantity,
                    UnitPrice = items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                }).ToList(),
                TotalAmount = totalAmount
            };

            _logger.LogInformation("Checkout GET action called. Order total amount: {TotalAmount}", order.TotalAmount);

            // Pass the client secret to the view
            ViewBag.PaymentIntentClientSecret = paymentIntentClientSecret;

            return View(order);
        }

        /// <summary>
        /// Handles the checkout form submission, confirms the payment, and processes the order.
        /// </summary>
        /// <param name="order">Order model containing shipping details.</param>
        /// <param name="paymentIntentId">Payment Intent ID received from the client.</param>
        /// <returns>Redirects to OrderConfirmation on success; otherwise, returns to Checkout view with errors.</returns>
        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order, string paymentIntentId)
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

            // Remove OrderItems, PaymentIntentId, and TotalAmount from model binding since they come from the cart and processing
            ModelState.Remove(nameof(Order.OrderItems));
            ModelState.Remove(nameof(Order.PaymentIntentId));
            ModelState.Remove(nameof(Order.TotalAmount)); // Remove TotalAmount to prevent client-side tampering

            // Extract all ItemIds from the cart
            var itemIds = cart.Select(c => c.ItemId).ToList();

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
                    .Where(c => items.Any(i => i.ItemId == c.ItemId))
                    .Select(c => new OrderItem
                    {
                        ItemId = c.ItemId,
                        Quantity = c.Quantity,
                        UnitPrice = items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                    }).ToList();
                order.TotalAmount = order.OrderItems.Sum(c => c.UnitPrice * c.Quantity);

                _logger.LogInformation("After removing missing items, OrderTotalAmount: {TotalAmount}", order.TotalAmount);

                return View(order);
            }

            // Calculate TotalAmount and log each item's details
            decimal calculatedTotal = 0.0m;
            foreach (var c in cart)
            {
                var item = items.FirstOrDefault(i => i.ItemId == c.ItemId);
                if (item != null)
                {
                    _logger.LogInformation("Cart Item - ID: {ItemId}, Name: {ItemName}, Price: {UnitPrice}, Quantity: {Quantity}",
                        item.ItemId,
                        item.Name,
                        item.Price,
                        c.Quantity);
                    calculatedTotal += item.Price * c.Quantity;
                }
            }
            _logger.LogInformation("Calculated Total Amount: {CalculatedTotal}", calculatedTotal);

            // Validate that TotalAmount is at least $0.50 (Stripe requirement for Payment Intents)
            if (calculatedTotal < 0.50m)
            {
                ModelState.AddModelError("", "Order total must be at least $0.50.");
                _logger.LogWarning("Checkout POST action: Order total {TotalAmount} is invalid.", calculatedTotal);

                // Reconstruct OrderItems with Items
                order.OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    Quantity = c.Quantity,
                    UnitPrice = items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                }).ToList();
                order.TotalAmount = calculatedTotal;

                return View(order);
            }

            // Assign calculated total to order
            order.TotalAmount = calculatedTotal;

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Proceeding to confirm payment and save order.");

                // Retrieve the existing Payment Intent using the PaymentIntent ID
                var service = new PaymentIntentService();
                PaymentIntent paymentIntent = null;
                try
                {
                    paymentIntent = await service.GetAsync(paymentIntentId);
                }
                catch (StripeException ex)
                {
                    _logger.LogError("Stripe Error while retrieving PaymentIntent: {Message}", ex.Message);
                    ModelState.AddModelError("", "Payment retrieval failed. Please try again.");
                    return View(order);
                }

                if (paymentIntent == null)
                {
                    ModelState.AddModelError("", "Invalid Payment Intent.");
                    _logger.LogError("PaymentIntent with ID {PaymentIntentId} not found.", paymentIntentId);
                    return View(order);
                }

                _logger.LogInformation("PaymentIntent {PaymentIntentId} succeeded.", paymentIntent.Id);

                // Save shipping details
                _context.ShippingDetails.Add(order.ShippingDetails);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Shipping details saved with ID: {ShippingDetailsId}", order.ShippingDetails.ShippingDetailsId);

                // Assign the Item properties to OrderItems
                order.OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    Item = items.FirstOrDefault(i => i.ItemId == c.ItemId),
                    Quantity = c.Quantity,
                    UnitPrice = items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                }).ToList();
                order.PaymentIntentId = paymentIntent.Id; // Set PaymentIntentId

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
                .Where(c => itemsInCart.Any(i => i.ItemId == c.ItemId))
                .Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    Quantity = c.Quantity,
                    UnitPrice = itemsInCart.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                }).ToList();
            order.TotalAmount = order.OrderItems.Sum(c => c.UnitPrice * c.Quantity);

            _logger.LogInformation("After ModelState invalid, OrderTotalAmount: {TotalAmount}", order.TotalAmount);

            return View(order);
        }

        /// <summary>
        /// Displays the order confirmation page with order details.
        /// </summary>
        /// <param name="id">Order ID.</param>
        /// <returns>OrderConfirmation view with Order model.</returns>
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

        /// <summary>
        /// Removes an item from the shopping cart based on Item ID.
        /// </summary>
        /// <param name="id">Item ID to remove.</param>
        /// <returns>Redirects to the Index action.</returns>
        // POST: Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Find the cart item by ItemId
            var cartItem = cart.FirstOrDefault(c => c.ItemId == id);

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
        /// Creates a Stripe Payment Intent for the specified amount and currency.
        /// </summary>
        /// <param name="amount">Total amount in USD.</param>
        /// <param name="currency">Currency code (default is "usd").</param>
        /// <returns>Client secret of the Payment Intent if successful; otherwise, null.</returns>
        private async Task<string> CreateStripePaymentIntentAsync(decimal amount, string currency = "usd")
        {
            try
            {
                if (amount < 0.50m)
                {
                    _logger.LogError("Stripe Error: Amount {Amount} is less than the minimum required.", amount);
                    return null;
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Amount in cents
                    Currency = currency,
                    PaymentMethodTypes = new List<string>
                    {
                        "card",
                    },
                    Description = "Order Payment",
                };

                var service = new PaymentIntentService();
                PaymentIntent paymentIntent = await service.CreateAsync(options);

                return paymentIntent.ClientSecret;
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