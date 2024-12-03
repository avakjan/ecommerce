using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OnlineShoppingSite.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeApiController : ControllerBase
    {
        private readonly ILogger<HomeApiController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeApiController(ILogger<HomeApiController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: api/home/featured-products
        [HttpGet("featured-products")]
        public async Task<ActionResult<IEnumerable<Item>>> GetFeaturedProducts()
        {
            try
            {
                var featuredProducts = await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.ItemSizes)
                        .ThenInclude(isz => isz.Size)
                    .Take(6) // Limit to 6 featured products
                    .ToListAsync();

                return Ok(featuredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured products");
                return StatusCode(500, new { error = "Error fetching featured products" });
            }
        }

        // GET: api/home/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Items)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");
                return StatusCode(500, new { error = "Error fetching categories" });
            }
        }

        // GET: api/home/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Item>>> Search([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest(new { error = "Search query is required" });

                var searchResults = await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.ItemSizes)
                        .ThenInclude(isz => isz.Size)
                    .Where(i => i.Name.Contains(query) || 
                               i.Description.Contains(query) || 
                               i.Category.Name.Contains(query))
                    .ToListAsync();

                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search");
                return StatusCode(500, new { error = "Error performing search" });
            }
        }

        // GET: api/home/health
        [HttpGet("health")]
        public ActionResult GetHealthCheck()
        {
            try
            {
                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { error = "Health check failed" });
            }
        }

        // GET: api/home/error
        [HttpGet("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult GetError()
        {
            return StatusCode(500, new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        // GET: api/home/privacy
        [HttpGet("privacy")]
        public ActionResult GetPrivacyPolicy()
        {
            try
            {
                // You might want to load this from a configuration or database
                return Ok(new
                {
                    title = "Privacy Policy",
                    lastUpdated = "2024-01-01",
                    content = "Our privacy policy content..."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching privacy policy");
                return StatusCode(500, new { error = "Error fetching privacy policy" });
            }
        }

        // GET: api/home/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetSiteStats()
        {
            try
            {
                var stats = new
                {
                    totalProducts = await _context.Items.CountAsync(),
                    totalCategories = await _context.Categories.CountAsync(),
                    totalOrders = await _context.Orders.CountAsync(),
                    featuredProducts = await _context.Items
                        .OrderByDescending(i => i.ItemId)
                        .Take(5)
                        .Select(i => new { i.Name, i.Price })
                        .ToListAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching site statistics");
                return StatusCode(500, new { error = "Error fetching site statistics" });
            }
        }
    }
}
