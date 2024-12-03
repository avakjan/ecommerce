using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using OnlineShoppingSite.Extensions;
using Microsoft.AspNetCore.Identity;
using Stripe;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace OnlineShoppingSite.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartApiController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartApiController(
            ApplicationDbContext context,
            ILogger<CartApiController> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        // GET: api/cart/index
        [HttpGet("index")]
        public ActionResult<CartViewModel> Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            _logger.LogInformation("Cart Index action called. Cart has {ItemCount} items.", cart.Count);

            if (!cart.Any())
            {
                return Ok(new CartViewModel
                {
                    CartItems = new List<CartItem>(),
                    Items = new List<Item>(),
                    Sizes = new List<Size>()
                });
            }

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

        // GET: api/cart/checkout
        [HttpGet("checkout")]
        public async Task<ActionResult<CheckoutViewModel>> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (!cart.Any())
            {
                _logger.LogWarning("Checkout attempted with empty cart.");
                return BadRequest(new { error = "Your cart is empty." });
            }

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

            // Validate items and sizes
            var missingItemIds = itemIds.Except(items.Select(i => i.ItemId)).ToList();
            var missingSizeIds = sizeIds.Except(sizes.Select(s => s.SizeId)).ToList();

            if (missingItemIds.Any() || missingSizeIds.Any())
            {
                return BadRequest(new { error = "Some items or sizes in your cart are no longer available." });
            }

            // Validate quantities
            foreach (var cartItem in cart)
            {
                var item = items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                var itemSize = item.ItemSizes.FirstOrDefault(isz => isz.SizeId == cartItem.SizeId);
                if (itemSize.Quantity < cartItem.Quantity)
                {
                    return BadRequest(new { error = $"Insufficient quantity for {item.Name} (Size: {itemSize.Size.Name})." });
                }
            }

            decimal totalAmount = cart.Sum(c => 
            {
                var item = items.FirstOrDefault(i => i.ItemId == c.ItemId);
                return item != null ? item.Price * c.Quantity : 0;
            });

            if (totalAmount < 0.50m)
            {
                return BadRequest(new { error = "Order total must be at least 0.50€." });
            }

            var paymentIntentClientSecret = await CreateStripePaymentIntentAsync(totalAmount);

            if (string.IsNullOrEmpty(paymentIntentClientSecret))
            {
                return BadRequest(new { error = "Unable to process payment at this time. Please try again later." });
            }

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

            return Ok(viewModel);
        }

        // POST: api/cart/checkout
        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutViewModel model)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var userId = _userManager.GetUserId(User);

            if (!cart.Any())
            {
                return BadRequest(new { error = "Your cart is empty." });
            }

            // Reconstruct OrderItems and validate inventory
            var itemIds = cart.Select(c => c.ItemId).ToList();
            var items = await _context.Items
                .Include(i => i.ItemSizes)
                .Where(i => itemIds.Contains(i.ItemId))
                .ToListAsync();

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Update inventory and validate quantities
                foreach (var cartItem in cart)
                {
                    var itemSize = await _context.ItemSizes
                        .Where(isz => isz.ItemId == cartItem.ItemId && isz.SizeId == cartItem.SizeId)
                        .FirstOrDefaultAsync();

                    if (itemSize == null || itemSize.Quantity < cartItem.Quantity)
                    {
                        return BadRequest(new { error = "Insufficient quantity available." });
                    }

                    itemSize.Quantity -= cartItem.Quantity;
                    _context.ItemSizes.Update(itemSize);
                }

                // Create and save order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    ShippingDetails = model.ShippingDetails,
                    PaymentMethod = model.PaymentMethod,
                    OrderItems = cart.Select(c => new OrderItem
                    {
                        ItemId = c.ItemId,
                        SizeId = c.SizeId,
                        Quantity = c.Quantity,
                        UnitPrice = items.FirstOrDefault(i => i.ItemId == c.ItemId)?.Price ?? 0
                    }).ToList(),
                    Status = "Pending"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Clear cart
                HttpContext.Session.Remove("Cart");

                return Ok(new { 
                    orderId = order.OrderId,
                    message = "Order placed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                return BadRequest(new { error = "Error processing order. Please try again." });
            }
        }

        // GET: api/cart/orderconfirmation/{id}
        [HttpGet("orderconfirmation/{id}")]
        [Authorize]
        public async Task<ActionResult<Order>> OrderConfirmation(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
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
                return NotFound(new { error = "Order not found" });
            }

            if (order.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(order);
        }

        // POST: api/cart/remove
        [HttpPost("remove")]
        public IActionResult Remove([FromBody] CartRemoveModel model)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.ItemId == model.ItemId && c.SizeId == model.SizeId);

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                return Ok(new { message = "Item removed from cart" });
            }

            return NotFound(new { error = "Item not found in cart" });
        }

        // POST: api/cart/updatequantities
        [HttpPost("updatequantities")]
        public IActionResult UpdateQuantities([FromBody] CartViewModel model)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            foreach (var updatedItem in model.CartItems)
            {
                var cartItem = cart.FirstOrDefault(ci => ci.ItemId == updatedItem.ItemId && ci.SizeId == updatedItem.SizeId);
                if (cartItem != null)
                {
                    cartItem.Quantity = updatedItem.Quantity > 0 ? updatedItem.Quantity : 1;
                }
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Ok(new { message = "Cart quantities updated" });
        }

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
                    Amount = (long)(amount * 100),
                    Currency = currency,
                    PaymentMethodTypes = new List<string> { "card" },
                    Description = "Order Payment",
                };

                var service = new PaymentIntentService();
                PaymentIntent paymentIntent = await service.CreateAsync(options);
                return paymentIntent.ClientSecret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return null;
            }
        }
    }

    public class CartRemoveModel
    {
        public int ItemId { get; set; }
        public int SizeId { get; set; }
    }
}
