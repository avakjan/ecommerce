using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OnlineShoppingSite.Models;
using OnlineShoppingSite.ViewModels;

namespace OnlineShoppingSite.Controllers
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

        // GET: api/home
        // Instead of returning a Razor view, return a simple JSON message
        [HttpGet]
        public IActionResult GetHomeMessage()
        {
            return Ok(new { Message = "Welcome to the Online Shopping Site API!" });
        }

        // GET: api/home/privacy
        // Returns some basic info about privacy, or a link to a policy
        [HttpGet("privacy")]
        public IActionResult Privacy()
        {
            // Could return a JSON with a link to your privacy policy or text
            return Ok(new { Policy = "This is our privacy policy text or link." });
        }

        // GET: api/home/error
        // Returns error details in JSON instead of a Razor view
        [HttpGet("error")]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            };

            _logger.LogError("Error endpoint called with RequestId: {RequestId}", errorViewModel.RequestId);

            return Problem(
                detail: $"RequestId: {errorViewModel.RequestId}",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An error occurred."
            );
        }
    }
}