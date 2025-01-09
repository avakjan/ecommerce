using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.Extensions;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSite.ViewModels;
using Stripe;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace OnlineShoppingSite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            ApplicationDbContext context,
            ILogger<CartController> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;

            // Initialize Stripe with the Secret Key from configuration
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        /// <summary>
        /// Retrieve the current cart items from Session and return as JSON.
        /// </summary>
        /// <returns>JSON list of CartItems plus item/size info.</returns>
        // GET: api/cart
        [HttpGet]
        public IActionResult GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            _logger.LogInformation("Cart has {ItemCount} items.", cart.Count);

            if (cart.Any())
            {
                var itemIds = cart.Select(c => c.ItemId).Distinct().ToList();
                var sizeIds = cart.Select(c => c.SizeId).Distinct().ToList();

                var items = _context.Items
                    .Include(i => i.ItemSizes)
                        .ThenInclude(isz => isz.Size)
                    .Where(i => itemIds.Contains(i.ItemId))
                    .ToList();

                var sizes = _context.Sizes
                    .Where(s => sizeIds.Contains(s.SizeId))
                    .ToList();

                var cartViewModel = new CartViewModel
                {
                    CartItems = cart,
                    Items = items,
                    Sizes = sizes
                };

                // Calculate TotalAmount
                decimal totalAmount = 0;
                foreach (var cartItem in cart)
                {
                    var item = items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                    if (item != null)
                    {
                        totalAmount += item.Price * cartItem.Quantity;
                    }
                }
                cartViewModel.TotalAmount = totalAmount;

                return Ok(cartViewModel);
            }
            else
            {
                var emptyViewModel = new CartViewModel
                {
                    CartItems = new List<CartItem>(),
                    Items = new List<Item>(),
                    Sizes = new List<Size>(),
                    TotalAmount = 0
                };
                return Ok(emptyViewModel);
            }
        }

        /// <summary>
        /// Initiates checkout flow by validating cart items, creating a PaymentIntent, etc.
        /// </summary>
        /// <returns>Checkout information (PaymentIntent client secret, total, items, etc.)</returns>
        // GET: api/cart/checkout
        [HttpGet("checkout")]
        public async Task<IActionResult> StartCheckout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any())
            {
                _logger.LogWarning("Checkout attempted with empty cart.");
                return BadRequest(new { Error = "Your cart is empty." });
            }

            var itemIds = cart.Select(c => c.ItemId).ToList();
            var sizeIds = cart.Select(c => c.SizeId).Distinct().ToList();

            var items = await _context.Items
                .Include(i => i.ItemSizes).ThenInclude(isz => isz.Size)
                .Where(i => itemIds.Contains(i.ItemId))
                .ToListAsync();

            var sizes = await _context.Sizes
                .Where(s => sizeIds.Contains(s.SizeId))
                .ToListAsync();

            // Check if any Items or Sizes are missing
            var missingItemIds = itemIds.Except(items.Select(i => i.ItemId)).ToList();
            var missingSizeIds = sizeIds.Except(sizes.Select(s => s.SizeId)).ToList();
            if (missingItemIds.Any() || missingSizeIds.Any())
            {
                _logger.LogWarning(
                    "Checkout: Missing items or sizes. MissingItemIds: {MissingItemIds}, MissingSizeIds: {MissingSizeIds}",
                    string.Join(", ", missingItemIds), string.Join(", ", missingSizeIds)
                );
                return BadRequest(new { Error = "Some items or sizes in your cart are no longer available." });
            }

            // Validate stock
            foreach (var cartItem in cart)
            {
                var item = items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                if (item == null) continue;

                var itemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == cartItem.SizeId);
                if (itemSize == null || itemSize.Quantity < cartItem.Quantity)
                {
                    _logger.LogWarning(
                        "Insufficient quantity for ItemId {ItemId}, SizeId {SizeId}. Requested: {Req}, Available: {Avail}",
                        cartItem.ItemId, cartItem.SizeId, cartItem.Quantity, itemSize?.Quantity ?? 0
                    );
                    return BadRequest(new { Error = $"Insufficient quantity for {item.Name} (SizeId: {cartItem.SizeId})." });
                }
            }

            // Calculate total
            decimal totalAmount = cart.Sum(c =>
            {
                var item = items.FirstOrDefault(i => i.ItemId == c.ItemId);
                return item != null ? item.Price * c.Quantity : 0;
            });

            // Stripe requires a minimum of 0.50
            if (totalAmount < 0.50m)
            {
                _logger.LogWarning("Order total {TotalAmount} is below minimum (0.50).", totalAmount);
                return BadRequest(new { Error = "Order total must be at least 0.50€." });
            }

            // Create a PaymentIntent
            var paymentIntentClientSecret = await CreateStripePaymentIntentAsync(totalAmount);
            if (string.IsNullOrEmpty(paymentIntentClientSecret))
            {
                return StatusCode(500, new { Error = "Unable to create payment intent. Try again later." });
            }

            // Return a summary of the checkout
            var checkoutSummary = new
            {
                CartItems = cart,
                TotalAmount = totalAmount,
                PaymentIntentClientSecret = paymentIntentClientSecret
            };
            return Ok(checkoutSummary);
        }

        /// <summary>
        /// Finalizes the checkout: updates inventory, saves the order, clears cart, etc.
        /// Must be called after the payment is confirmed on the client side (Stripe).
        /// </summary>
        // POST: api/cart/checkout
        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> CompleteCheckout([FromBody] CheckoutViewModel model)
        {
            _logger.LogInformation("Checkout POST action called.");

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var userId = _userManager.GetUserId(User);

            if (!cart.Any())
            {
                _logger.LogWarning("Checkout POST action: cart is empty.");
                return BadRequest(new { Error = "Your cart is empty." });
            }

            // Rebuild OrderItems from cart
            model.OrderItems = cart
                .Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    SizeId = c.SizeId,
                    Quantity = c.Quantity,
                    UnitPrice = 0 // will fill in below
                })
                .ToList();

            // Fetch items for price info / stock checks
            var itemIds = cart.Select(c => c.ItemId).ToList();
            var sizeIds = cart.Select(c => c.SizeId).Distinct().ToList();

            var items = await _context.Items
                .Include(i => i.ItemSizes).ThenInclude(isz => isz.Size)
                .Where(i => itemIds.Contains(i.ItemId))
                .ToListAsync();

            var sizes = await _context.Sizes
                .Where(s => sizeIds.Contains(s.SizeId))
                .ToListAsync();

            // Check missing items/sizes
            var missingItemIds = itemIds.Except(items.Select(i => i.ItemId)).ToList();
            var missingSizeIds = sizeIds.Except(sizes.Select(s => s.SizeId)).ToList();
            if (missingItemIds.Any() || missingSizeIds.Any())
            {
                _logger.LogWarning(
                    "Checkout POST: Missing items or sizes. MissingItemIds: {MissingItemIds}, MissingSizeIds: {MissingSizeIds}",
                    string.Join(", ", missingItemIds), string.Join(", ", missingSizeIds)
                );
                return BadRequest(new { Error = "Some items or sizes in your cart are no longer available." });
            }

            // Validate stock & decrement
            try
            {
                foreach (var cartItem in cart)
                {
                    var itemSize = await _context.ItemSizes
                        .Where(isz => isz.ItemId == cartItem.ItemId && isz.SizeId == cartItem.SizeId)
                        .FirstOrDefaultAsync();

                    if (itemSize == null || itemSize.Quantity < cartItem.Quantity)
                    {
                        _logger.LogWarning(
                            "Not enough items in stock for ItemId {ItemId} SizeId {SizeId}. Req: {Req}, Avail: {Avail}",
                            cartItem.ItemId, cartItem.SizeId, cartItem.Quantity, itemSize?.Quantity ?? 0
                        );
                        return BadRequest(new { Error = "Not enough items in stock." });
                    }

                    // Decrement quantity
                    itemSize.Quantity -= cartItem.Quantity;
                    _context.ItemSizes.Update(itemSize);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Inventory updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating inventory.");
                return StatusCode(409, new { Error = "A concurrency error occurred. Please try again." });
            }

            // Fill in UnitPrice from DB items
            foreach (var orderItem in model.OrderItems)
            {
                var item = items.FirstOrDefault(i => i.ItemId == orderItem.ItemId);
                if (item != null) orderItem.UnitPrice = item.Price;
            }

            // Calculate final total
            decimal calculatedTotal = model.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
            model.TotalAmount = calculatedTotal;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid in CompleteCheckout. Returning validation errors.");
                return BadRequest(ModelState);
            }

            // Create & save the order
            var order = new Order
            {
                ShippingDetails = model.ShippingDetails,
                PaymentMethod = model.PaymentMethod,
                OrderItems = model.OrderItems,
                TotalAmount = model.TotalAmount,
                UserId = userId,
                Status = "Pending"
            };

            // Save shipping details
            _context.ShippingDetails.Add(order.ShippingDetails);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Shipping details saved with ID: {Id}", order.ShippingDetails.ShippingDetailsId);

            // Attach shipping details ID
            order.ShippingDetailsId = order.ShippingDetails.ShippingDetailsId;

            // Save order
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order saved with ID: {OrderId}", order.OrderId);

            // Clear cart from session
            HttpContext.Session.Remove("Cart");
            _logger.LogInformation("Cart cleared from session after checkout.");

            // Return the created order info
            return Ok(new { Message = "Checkout complete", OrderId = order.OrderId });
        }

        /// <summary>
        /// Returns details of a single order (for confirmation or display).
        /// </summary>
        // GET: api/cart/orderConfirmation/{id}
        [HttpGet("orderConfirmation/{id}")]
        [Authorize]
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            _logger.LogInformation("OrderConfirmation called with OrderId: {OrderId}", id);

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("User not logged in or user ID not found.");
                return Unauthorized(new { Error = "User not logged in." });
            }

            var order = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found.", id);
                return NotFound(new { Error = "Order not found." });
            }

            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to access Order {OrderId} belonging to {OrderUserId}.",
                    currentUserId, id, order.UserId);
                return Forbid();
            }

            return Ok(order);
        }

        /// <summary>
        /// Removes an item from the cart by ItemId & SizeId.
        /// </summary>
        // DELETE: api/cart/item?itemId=xxx&sizeId=yyy
        [HttpDelete("item")]
        public IActionResult RemoveItemFromCart([FromQuery] int itemId, [FromQuery] int sizeId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.ItemId == itemId && c.SizeId == sizeId);

            if (cartItem == null)
            {
                _logger.LogWarning("Attempted to remove item {ItemId} size {SizeId} not in cart.", itemId, sizeId);
                return NotFound(new { Error = "Item not found in cart." });
            }

            cart.Remove(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            _logger.LogInformation("Removed item {ItemId}, size {SizeId} from cart.", itemId, sizeId);

            return Ok(new { Message = "Item removed from cart." });
        }

        /// <summary>
        /// Updates item quantities in the cart. 
        /// Expects a CartViewModel with updated CartItems in JSON.
        /// </summary>
        // PUT: api/cart/quantities
        [HttpPut("quantities")]
        public IActionResult UpdateQuantities([FromBody] CartViewModel model)
        {
            // Grab the session cart
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Update each item’s quantity
            foreach (var updatedItem in model.CartItems)
            {
                var existing = cart.FirstOrDefault(ci => ci.ItemId == updatedItem.ItemId && ci.SizeId == updatedItem.SizeId);
                if (existing != null)
                {
                    // Ensure at least 1
                    existing.Quantity = updatedItem.Quantity > 0 ? updatedItem.Quantity : 1;
                }
            }

            // Save back to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            _logger.LogInformation("Cart quantities updated for {Count} items.", model.CartItems.Count);
            return Ok(new { Message = "Cart quantities updated." });
        }

        #region Private Helper Methods

        /// <summary>
        /// Creates a Stripe Payment Intent and returns its Client Secret if successful.
        /// </summary>
        private async Task<string> CreateStripePaymentIntentAsync(decimal amount, string currency = "eur")
        {
            try
            {
                if (amount < 0.50m)
                {
                    _logger.LogError("Stripe Error: Amount {Amount} < 0.50", amount);
                    return null;
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert to cents
                    Currency = currency,
                    PaymentMethodTypes = new List<string> { "card" },
                    Description = "Order Payment"
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

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

        #endregion
    }
}