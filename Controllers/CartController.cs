using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.Extensions;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSite.ViewModels;
using Stripe;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace OnlineShoppingSite.Controllers.Api
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

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpGet]
        public IActionResult GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            _logger.LogInformation("Cart retrieved. Count: {count}", cart.Count);

            if (!cart.Any())
            {
                return Ok(new { items = new List<CartItem>(), total = 0 });
            }

            // Calculate total
            var itemIds = cart.Select(c => c.ItemId).ToList();
            var items = _context.Items.Where(i => itemIds.Contains(i.ItemId)).ToList();

            decimal total = cart.Sum(c =>
            {
                var item = items.FirstOrDefault(i => i.ItemId == c.ItemId);
                return item == null ? 0 : item.Price * c.Quantity;
            });

            return Ok(new { items = cart, total = total });
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutViewModel model)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var userId = _userManager.GetUserId(User);

            if (!cart.Any())
            {
                return BadRequest("Your cart is empty.");
            }

            // Validate items and calculate total
            var itemIds = cart.Select(c => c.ItemId).ToList();
            var items = await _context.Items
                .Include(i => i.ItemSizes)
                .ThenInclude(isz => isz.Size)
                .Where(i => itemIds.Contains(i.ItemId))
                .ToListAsync();

            // Check for missing items or stock
            foreach (var cartItem in cart)
            {
                var item = items.FirstOrDefault(i => i.ItemId == cartItem.ItemId);
                if (item == null)
                {
                    return BadRequest($"Item with ID {cartItem.ItemId} no longer available.");
                }
                var itemSize = item.ItemSizes.FirstOrDefault(s => s.SizeId == cartItem.SizeId);
                if (itemSize == null || itemSize.Quantity < cartItem.Quantity)
                {
                    return BadRequest($"Insufficient stock for {item.Name} (SizeId: {cartItem.SizeId}).");
                }
            }

            decimal totalAmount = cart.Sum(c =>
            {
                var item = items.FirstOrDefault(i => i.ItemId == c.ItemId);
                return item != null ? item.Price * c.Quantity : 0;
            });

            if (totalAmount < 0.50m)
            {
                return BadRequest("Order total must be at least 0.50€.");
            }

            // Create Payment Intent with Stripe
            var paymentIntentClientSecret = await CreateStripePaymentIntentAsync(totalAmount);
            if (string.IsNullOrEmpty(paymentIntentClientSecret))
            {
                return StatusCode(500, "Unable to process payment at this time.");
            }

            // Adjust stock
            foreach (var cItem in cart)
            {
                var item = items.First(i => i.ItemId == cItem.ItemId);
                var itemSize = item.ItemSizes.First(isz => isz.SizeId == cItem.SizeId);
                itemSize.Quantity -= cItem.Quantity;
                _context.ItemSizes.Update(itemSize);
            }

            await _context.SaveChangesAsync();

            // Create and save order
            var order = new Order
            {
                ShippingDetails = model.ShippingDetails,
                PaymentMethod = model.PaymentMethod,
                OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    SizeId = c.SizeId,
                    Quantity = c.Quantity,
                    UnitPrice = items.First(i => i.ItemId == c.ItemId).Price
                }).ToList(),
                TotalAmount = totalAmount,
                UserId = userId,
                Status = "Pending"
            };

            _context.ShippingDetails.Add(order.ShippingDetails);
            await _context.SaveChangesAsync();

            order.ShippingDetailsId = order.ShippingDetails.ShippingDetailsId;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear cart
            HttpContext.Session.Remove("Cart");
            _logger.LogInformation("Order {OrderId} created and cart cleared.", order.OrderId);

            return Ok(new { message = "Order placed successfully.", orderId = order.OrderId });
        }

        [HttpDelete("remove")]
        public IActionResult RemoveItemFromCart(int itemId, int sizeId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.ItemId == itemId && c.SizeId == sizeId);

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                return Ok("Item removed from cart.");
            }

            return NotFound("Item not found in cart.");
        }

        [HttpPut("update")]
        public IActionResult UpdateQuantities([FromBody] List<CartItem> updatedCartItems)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            foreach (var updatedItem in updatedCartItems)
            {
                var cartItem = cart.FirstOrDefault(ci => ci.ItemId == updatedItem.ItemId && ci.SizeId == updatedItem.SizeId);
                if (cartItem != null)
                {
                    cartItem.Quantity = updatedItem.Quantity > 0 ? updatedItem.Quantity : 1;
                }
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Ok("Cart updated.");
        }

        private async Task<string> CreateStripePaymentIntentAsync(decimal amount, string currency = "eur")
        {
            try
            {
                if (amount < 0.50m) return null;

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100),
                    Currency = currency,
                    PaymentMethodTypes = new List<string> { "card" },
                    Description = "Order Payment",
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
    }
}