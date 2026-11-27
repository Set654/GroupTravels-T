using Microsoft.AspNetCore.Mvc;

namespace TripService.Api.Controllers;

[ApiController]
[Route("api/health/live")]
public sealed class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    public HealthController(ILogger<HealthController> logger) => _logger = logger;

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Health endpoint requested");
        return Ok(new { status = "ok", service = "TripService.Api" });
    }
}
