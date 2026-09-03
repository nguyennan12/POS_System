using Microsoft.AspNetCore.Mvc;

namespace POS.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", time = DateTime.UtcNow });
}
