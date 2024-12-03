using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountApiController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountApiController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        // POST: api/account/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User registered and signed in.");

                    return Ok(new { message = "Registration successful" });
                }

                return BadRequest(new { errors = result.Errors });
            }

            return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        // POST: api/account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    var roles = await _userManager.GetRolesAsync(user);

                    return Ok(new 
                    { 
                        message = "Login successful",
                        user = new
                        {
                            email = user.Email,
                            roles = roles
                        }
                    });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return BadRequest(new { error = "Account is locked out" });
                }

                return BadRequest(new { error = "Invalid login attempt" });
            }

            return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        // POST: api/account/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return Ok(new { message = "Logout successful" });
        }

        // GET: api/account/orders
        [Authorize]
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
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

        // GET: api/account/orders/{id}
        [Authorize]
        [HttpGet("orders/{id}")]
        public async Task<ActionResult<Order>> GetOrderDetails(int id)
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
                return NotFound();
            }

            return Ok(order);
        }

        // GET: api/account/profile
        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<ApplicationUser>> GetProfile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                email = user.Email,
                userName = user.UserName,
                roles = roles
            });
        }

        // PUT: api/account/profile
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update user properties as needed
                user.Email = model.Email;
                user.UserName = model.Email;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return NoContent();
                }

                return BadRequest(new { errors = result.Errors });
            }

            return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        // POST: api/account/change-password
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { message = "Password changed successfully" });
            }

            return BadRequest(new { errors = result.Errors });
        }
    }

    public class UpdateProfileViewModel
    {
        public string Email { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
