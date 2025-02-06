// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.Extensions;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSite.ViewModels;
using Stripe;
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

        public CartController(ApplicationDbContext context, ILogger<CartController> logger, IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;

            // Initialize Stripe with the Secret Key from configuration
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            _logger.LogInformation("Cart Index action called. Cart has {ItemCount} items.", cart.Count);

            if (cart.Any())
            {
                var viewModel = new CartViewModel
                {
                    CartItems = cart,
                    Items = _context.Items
                                .Include(i => i.ItemSizes)
                                    .ThenInclude(isz => isz.Size)
                                .Where(i => cart.Select(c => c.ItemId).Contains(i.ItemId))
                                .ToList(),
                    Sizes = _context.Sizes
                                .Where(s => cart.Select(c => c.SizeId).Contains(s.SizeId))
                                .ToList()
                };

                return Ok(viewModel);
            }
            else
            {
                var emptyViewModel = new CartViewModel
                {
                    CartItems = new List<CartItem>(),
                    Items = new List<Item>(),
                    Sizes = new List<Size>()
                };
                return Ok(emptyViewModel);
            }
        }

        public async Task<IActionResult> Checkout()
        {
            // Retrieve cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (!cart.Any())
            {
                _logger.LogWarning("Checkout attempted with empty cart.");
                return BadRequest(new { error = "Your cart is empty." });
            }

            // Extract all ItemIds and SizeIds from the cart
            var itemIds = cart.Select(c => c.ItemId).ToList();
            var sizeIds = cart.Select(c => c.SizeId).Distinct().ToList();

            // Fetch all corresponding Items and Sizes from the database
            var items = await _context.Items
                                      .Include(i => i.ItemSizes)
                                      .ThenInclude(isz => isz.Size)
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
                _logger.LogWarning("Checkout GET action: Missing Items or Sizes. MissingItemIds: {MissingItemIds}, MissingSizeIds: {MissingSizeIds}",
                    string.Join(", ", missingItemIds), string.Join(", ", missingSizeIds));
                return BadRequest(new { error = "Some items or sizes in your cart are no longer available." });
            }

            // Validate quantities
            foreach (var cartItem in cart)
            {
                var item = items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                var itemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == cartItem.SizeId);
                if (itemSize.Quantity < cartItem.Quantity)
                {
                    _logger.LogWarning("Checkout GET action: Insufficient quantity for ItemId {ItemId} SizeId {SizeId}. Requested: {Requested}, Available: {Available}.",
                        cartItem.ItemId, cartItem.SizeId, cartItem.Quantity, itemSize.Quantity);
                    return BadRequest(new { error = $"Insufficient quantity for {item.Name} (Size: {itemSize.Size.Name})." });
                }
            }

            // Calculate TotalAmount
            decimal totalAmount = cart.Sum(c => 
                {
                    var item = items.FirstOrDefault(i => i.ItemId == c.ItemId);
                    return item != null ? item.Price * c.Quantity : 0;
                });

            // Validate that TotalAmount is at least 0.50€ (Stripe requirement for Payment Intents)
            if (totalAmount < 0.50m)
            {
                _logger.LogWarning("Checkout GET action: Order total {TotalAmount} is below minimum.", totalAmount);
                return BadRequest(new { error = "Order total must be at least 0.50€." });
            }

            // Create Payment Intent
            var paymentIntentClientSecret = await CreateStripePaymentIntentAsync(totalAmount);

            if (string.IsNullOrEmpty(paymentIntentClientSecret))
            {
                return BadRequest(new { error = "Unable to process payment at this time. Please try again later." });
            }

            // Create the ViewModel
            var viewModel = new CheckoutViewModel
            {
                ShippingDetails = new ShippingDetails(),
                PaymentMethod = "Credit Card",
                OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    SizeId = c.SizeId,
                    Item = items.FirstOrDefault(i => i.ItemId == c.ItemId),
                    Quantity = c.Quantity,
                    UnitPrice = items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                }).ToList(),
                TotalAmount = totalAmount,
                PaymentIntentClientSecret = paymentIntentClientSecret
            };

            _logger.LogInformation("Checkout GET action called. Order total amount: {TotalAmount}", viewModel.TotalAmount);

            return Ok(viewModel);
        }

        [HttpPost("Checkout")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutViewModel model)
        {
            _logger.LogInformation("Checkout POST action called.");

            // Retrieve cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var userId = _userManager.GetUserId(User);
            _logger.LogInformation("Cart retrieved from session. Cart has {ItemCount} items.", cart.Count);

            // Check if cart is empty
            if (!cart.Any())
            {
                _logger.LogWarning("Checkout POST action: cart is empty.");
                return BadRequest(new { error = "Your cart is empty." });
            }

            // Reconstruct OrderItems from cart
            model.OrderItems = cart.Select(c => new OrderItem
            {
                ItemId = c.ItemId,
                SizeId = c.SizeId,
                Quantity = c.Quantity,
                UnitPrice = 0 // set later after fetching items
            }).ToList();

            // Fetch all Items and Sizes from the database
            var itemIds = cart.Select(c => c.ItemId).ToList();
            var sizeIds = cart.Select(c => c.SizeId).Distinct().ToList();

            var items = await _context.Items
                                    .Include(i => i.ItemSizes)
                                        .ThenInclude(isz => isz.Size)
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
                    "Checkout POST action: Missing Items or Sizes. MissingItemIds: {MissingItemIds}, MissingSizeIds: {MissingSizeIds}",
                    string.Join(", ", missingItemIds),
                    string.Join(", ", missingSizeIds)
                );

                // Rebuild order items with only the existing items
                model.OrderItems = cart
                    .Where(c => items.Any(i => i.ItemId == c.ItemId))
                    .Select(c => new OrderItem
                    {
                        ItemId = c.ItemId,
                        SizeId = c.SizeId,
                        Quantity = c.Quantity,
                        UnitPrice = items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                    })
                    .ToList();

                model.TotalAmount = model.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
                _logger.LogInformation("After removing missing items, OrderTotalAmount: {TotalAmount}", model.TotalAmount);

                return BadRequest(new
                {
                    error = "Some items or sizes in your cart are no longer available.",
                    updatedCart = model
                });
            }

            // Validate quantities and update inventory
            try
            {
                foreach (var cartItem in cart)
                {
                    var itemSize = await _context.ItemSizes
                        .FirstOrDefaultAsync(isz => isz.ItemId == cartItem.ItemId && isz.SizeId == cartItem.SizeId);

                    // If the itemSize doesn't exist OR not enough stock
                    if (itemSize == null)
                    {
                        _logger.LogWarning("ItemSize not found (ItemId={ItemId}, SizeId={SizeId}).", cartItem.ItemId, cartItem.SizeId);
                        return BadRequest(new { error = "Some items in your cart no longer exist." });
                    }
                    if (itemSize.Quantity < cartItem.Quantity)
                    {
                        _logger.LogWarning(
                            "Insufficient quantity for ItemId {ItemId} SizeId {SizeId}. Requested: {Requested}, Available: {Available}.",
                            cartItem.ItemId, cartItem.SizeId, cartItem.Quantity, itemSize.Quantity
                        );
                        return BadRequest(new
                        {
                            error = $"Not enough items in stock for {itemSize.Item?.Name} (Size: {itemSize.Size?.Name})."
                        });
                    }

                    // If stock is okay, reduce the quantity
                    itemSize.Quantity -= cartItem.Quantity;
                    _context.ItemSizes.Update(itemSize);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Inventory updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating inventory.");
                return BadRequest(new { error = "A concurrency error occurred. Please try again." });
            }

            // Update UnitPrice for each OrderItem
            foreach (var orderItem in model.OrderItems)
            {
                var item = items.FirstOrDefault(i => i.ItemId == orderItem.ItemId);
                if (item != null)
                {
                    orderItem.UnitPrice = item.Price;
                }
            }

            // Calculate TotalAmount
            model.TotalAmount = model.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

            // Check ModelState validity
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e));

                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join("; ", errors));
                return BadRequest(new { error = "Validation errors occurred.", details = errors });
            }

            // Create and save the Order
            var order = new Order
            {
                ShippingDetails = model.ShippingDetails,
                PaymentMethod = model.PaymentMethod,
                OrderItems = model.OrderItems,
                TotalAmount = model.TotalAmount,
                UserId = userId,
                Status = "Pending"
            };

            // TODO: Payment processing logic here (assume success for now)

            // Save ShippingDetails
            _context.ShippingDetails.Add(order.ShippingDetails);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Shipping details saved with ID: {ShippingDetailsId}", order.ShippingDetails.ShippingDetailsId);

            // Assign ShippingDetailsId to the order and save the order
            order.ShippingDetailsId = order.ShippingDetails.ShippingDetailsId;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order saved with ID: {OrderId}", order.OrderId);

            // Clear the cart
            HttpContext.Session.Remove("Cart");
            _logger.LogInformation("Cart cleared from session.");

            // Return success response
            return Ok(new
            {
                message = "Order placed successfully.",
                orderId = order.OrderId
            });
        }
        
        [HttpGet("OrderConfirmation/{id}")]
        [Authorize]
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            _logger.LogInformation("OrderConfirmation action called with OrderId: {OrderId}", id);

            // Retrieve the current user's ID
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("OrderConfirmation: Unable to retrieve current user ID.");
                return Unauthorized(); // or Redirect to login
            }

            // Fetch the order, including related data
            var order = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Size) // Include Size
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                _logger.LogWarning("OrderConfirmation: Order not found with ID: {OrderId}", id);
                return NotFound();
            }

            // Check if the order belongs to the current user
            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("OrderConfirmation: User {UserId} attempted to access Order {OrderId} belonging to User {OrderUserId}.",
                    currentUserId, id, order.UserId);
                return Forbid(); // Returns 403 Forbidden
            }

            return Ok(order);
        }


        [HttpPost("Remove")]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int itemId, int sizeId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Find the cart item by ItemId and SizeId
            var cartItem = cart.FirstOrDefault(c => c.ItemId == itemId && c.SizeId == sizeId);

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                _logger.LogInformation("Item with ID {ItemId} and SizeId {SizeId} removed from cart.", itemId, sizeId);

                return Ok(new
                {
                    message = "Item removed from cart successfully",
                    itemId = itemId,
                    sizeId = sizeId
                });
            }
            else
            {
                _logger.LogWarning("Attempted to remove item with ID {ItemId} and SizeId {SizeId} which was not found in cart.", itemId, sizeId);

                return NotFound(new 
                {
                    error = "Item not found in cart.",
                    itemId = itemId,
                    sizeId = sizeId
                });
            }
        }

        [HttpPost("UpdateQuantities")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantities([FromBody] CartViewModel model)
        {
            // Retrieve cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!ModelState.IsValid)
            {
                // Collect validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(msg => !string.IsNullOrWhiteSpace(msg));

                _logger.LogWarning("UpdateQuantities called with invalid ModelState. Errors: {Errors}", string.Join("; ", errors));

                return BadRequest(new 
                { 
                    error = "Invalid model data.", 
                    details = errors 
                });
            }

            // Update quantities
            foreach (var updatedItem in model.CartItems)
            {
                var cartItem = cart.FirstOrDefault(ci => ci.ItemId == updatedItem.ItemId && ci.SizeId == updatedItem.SizeId);
                if (cartItem != null)
                {
                    // Ensure the quantity is at least 1
                    cartItem.Quantity = updatedItem.Quantity > 0 ? updatedItem.Quantity : 1;
                }
                else
                {
                    _logger.LogWarning(
                        "UpdateQuantities: No matching cart item found for ItemId {ItemId}, SizeId {SizeId}. " +
                        "Cannot update quantity.",
                        updatedItem.ItemId, updatedItem.SizeId
                    );
                }
            }

            // Save the updated cart back to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            _logger.LogInformation("Cart quantities updated successfully.");

            // Redirect to the cart index action
            return Ok(new 
            {
                message = "Quantities updated successfully.",
                updatedCart = cart
            });
        }
        
        /// <summary>
        /// Creates a Stripe Payment Intent for the specified amount and currency.
        /// </summary>
        /// <param name="amount">Total amount in EUR.</param>
        /// <param name="currency">Currency code (default is "eur").</param>
        /// <returns>Client secret of the Payment Intent if successful; otherwise, null.</returns>
        private async Task<string> CreateStripePaymentIntentAsync(decimal amount, string currency = "eur")
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