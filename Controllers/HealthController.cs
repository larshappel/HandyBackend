using Microsoft.AspNetCore.Mvc;

namespace HandyBackend.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            Console.WriteLine("Health check endpoint hit");
            return Ok(new { status = "Healthy" });
        }
    }
}
