// Controllers/AccountController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        // POST: Account/Register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");
                // Optionally sign in the user or send confirmation email, etc.
                return Ok(new { message = "Registration successful." });
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return BadRequest(ModelState);
        }

        // GET: Account/Login
        [HttpGet("login")]
        public IActionResult Login([FromQuery] string returnUrl = null)
        {
            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return Ok(model);
        }

        // GET: Account/OrderDetails/5
        [HttpGet("orderDetails/{id}")]
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                _logger.LogWarning("OrderDetails: Order with ID {OrderId} not found for user {UserId}.", id, userId);
                return NotFound(new { error = "Order not found." });
            }

            return Ok(order);
        }

        // POST: api/account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Attempt to sign in the user. Note: lockoutOnFailure is set to false.
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");

                // Determine the returnUrl. If the provided URL is not local, use a default.
                string returnUrl = (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                                    ? model.ReturnUrl
                                    : "/home/index";

                return Ok(new 
                { 
                    message = "Login successful.",
                    returnUrl 
                });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                // HTTP 423 Locked
                return StatusCode(423, new { error = "User account locked out." });
            }

            // If login fails for any other reason
            return BadRequest(new { error = "Invalid login attempt." });
        }

        // POST: Account/Logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            return Ok(new { message = "User logged out successfully." });
        }

        // GET: Account/MyOrders
        [HttpGet("myorders")]
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .ToListAsync();

            return Ok(orders);
        }
    }
}