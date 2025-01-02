using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
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

        // POST: api/Account/Register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return validation errors
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Optionally assign roles here, e.g.: await _userManager.AddToRoleAsync(user, "User");

                // Sign in the user immediately (optional; depends on your flow)
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User registered and signed in.");

                // Return a 201 Created with user data or a success message
                return Created("", new { Message = "Registration successful", User = user.Email });
            }

            // If creation failed, return the errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);
        }

        // POST: api/Account/Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                // Return success message or user data
                return Ok(new { Message = "Login successful", Email = model.Email });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return Unauthorized(new { Error = "Account locked out." });
            }

            return Unauthorized(new { Error = "Invalid login attempt." });
        }

        // POST: api/Account/Logout
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            return Ok(new { Message = "Logout successful." });
        }

        // GET: api/Account/OrderDetails/5
        [Authorize]
        [HttpGet("orderdetails/{id}")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { Error = "Order not found" });
            }

            return Ok(order);
        }

        // GET: api/Account/MyOrders
        [Authorize]
        [HttpGet("myorders")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.ShippingDetails)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Size)
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/Account/AccessDenied
        // (Rarely used in pure API scenarios, but included for completeness.)
        [HttpGet("accessdenied")]
        public IActionResult AccessDenied()
        {
            return Forbid(); 
        }
    }
}