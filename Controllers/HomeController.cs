using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace OnlineShoppingSite.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            // Just return a status check
            return Ok(new { status = "API is running" });
        }

        [HttpGet("error")]
        public IActionResult Error()
        {
            // If you need an error endpoint
            return Problem("An error occurred.");
        }
    }
}